using System.Collections.Generic;
using System.Threading;
using AYellowpaper.SerializedCollections;
using PrimeTween;
using UnityEngine;

/// <summary>
/// A lifecycle animation for a character (spawn, death, and so on), the mirror of <see cref="HealthBehaviorSO"/>.
/// Implementations have to stay stateless: all mutable state lives in <see cref="LifecycleAnimationContext"/>
/// and in <see cref="LifecycleVfxSession"/>, so a single shared asset can be referenced by several prefabs
/// with no per-instance Instantiate.
///
/// The animation is expressed per SINGLE target (<see cref="PrepareTarget"/> / <see cref="PlayTarget"/>) and
/// applied to every target in the context: the body and the registered satellites (parts of composite
/// enemies, hats, weapons), which live outside the pivot's hierarchy and would otherwise stay motionless.
/// Working per-target is also what lets a satellite registered mid-phase be hooked in.
/// </summary>
public abstract class LifecycleAnimationSO : ScriptableObject
{
    [Header("VFX")]
    [SerializeField] private VfxCueEventChannel _vfxChannel;
    [SerializeField] private VfxStopEventChannel _vfxStopChannel;

    // protected and not private: the package's drawer resolves the field with Type.GetField() on the
    // ASSET's type (a subclass), and .NET does not return inherited private fields — the drawer would get
    // null and throw a NullReferenceException on every Inspector repaint. Inherited protected fields it
    // does find. No subclass should read this directly.
    [Tooltip("The phase's VFX, grouped by inner moment. Specs of the same stage start together.")]
    [SerializeField] protected SerializedDictionary<LifecycleVfxStage, List<LifecycleVfxSpec>> _vfx = new();

    [Header("Follow-up loop")]
    [Tooltip("Once the phase is over, start the looping animation configured for that phase on the character.")]
    [SerializeField] private bool _startsLoopOnComplete = true;

    [Tooltip("Which looping phase to chain into. Ignored when the flag above is false.")]
    [SerializeField] private LifecyclePhase _loopPhaseOnComplete = LifecyclePhase.Idle;

    // Replaces an `if (phase == Spawn) StartLoop(Idle)` hard-wired into the animator: a future `PostDeath`
    // that needs to chain a different loop (or none) is configured on the asset, with no code change.
    public bool StartsLoopOnComplete => _startsLoopOnComplete;
    public LifecyclePhase LoopPhaseOnComplete => _loopPhaseOnComplete;

    /// <summary>
    /// Instantly applies the initial state to every target (e.g. the sprite already underwater).
    /// Called in the same frame the GameObject is activated, before rendering.
    ///
    /// Purely visual: the <see cref="LifecycleVfxStage.Prepare"/> VFX are raised by the caller with
    /// <see cref="BeginVfx"/> right afterwards, so the "state applied → effect" ordering is guaranteed and
    /// re-preparing an already prepared phase does not restart them.
    /// </summary>
    public virtual void Prepare(in LifecycleAnimationContext ctx)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        for (int i = 0; i < targets.Count; i++)
            PrepareTarget(targets[i]);
    }

    /// <summary>The initial state of a single target.</summary>
    public virtual void PrepareTarget(in LifecycleAnimationTarget target) { }

    /// <summary>
    /// Starts the tweens on a single target and returns the "carrying" one (the movement), the only one
    /// worth awaiting. <c>default</c> means "nothing to await".
    /// </summary>
    public virtual Tween PlayTarget(in LifecycleAnimationTarget target) => default;

    /// <summary>
    /// Opens the phase's VFX session and raises the <see cref="LifecycleVfxStage.Prepare"/> stage.
    /// Called exactly once per phase, right after <see cref="Prepare"/>.
    /// </summary>
    public void BeginVfx(LifecycleVfxSession session, in LifecycleAnimationContext ctx)
    {
        if (session == null) return;

        session.Begin(_vfxStopChannel);

        // Forgotten wiring: a loop with no stop channel would stay on forever. A single log per phase
        // (this method runs once per phase, not per frame), with no state to track.
        if (_vfxStopChannel == null && HasLoopingSpec())
            Debug.LogWarning($"[{name}] Looping VFX with no _vfxStopChannel assigned: they will never be stopped.", this);

        RaiseVfxStage(LifecycleVfxStage.Prepare, in ctx, session);
    }

    /// <summary>
    /// Plays the phase: the movement of every target, with the <see cref="LifecycleVfxStage.Movement"/>
    /// and <see cref="LifecycleVfxStage.End"/> VFX at their respective hooks.
    ///
    /// The persistent effects opened here and in <see cref="BeginVfx"/> are NOT stopped by this method:
    /// that is done by the <see cref="CharacterLifecycleAnimator"/>'s <c>finally</c>, which also covers
    /// token cancellation (a character destroyed halfway through emerging) and the phase that was prepared
    /// and never played.
    ///
    /// The <paramref name="token"/> is not consumed by the template but stays in the signature: it is the
    /// contract for a subclass that needs to await something, and the animator already propagates it.
    /// </summary>
    public virtual async Awaitable PlayAsync(
        LifecycleAnimationContext ctx, LifecycleVfxSession session, CancellationToken token)
    {
        RaiseVfxStage(LifecycleVfxStage.Movement, in ctx, session);
        await PlayAllTargetsAsync(ctx);
        RaiseVfxStage(LifecycleVfxStage.End, in ctx, session);
    }

    /// <summary>
    /// Starts <see cref="PlayTarget"/> on every target and awaits only the body's: they all share the
    /// same duration, so they finish together and no <c>Sequence</c> is needed.
    /// </summary>
    protected async Awaitable PlayAllTargetsAsync(LifecycleAnimationContext ctx)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        if (targets.Count == 0) return;

        for (int i = 1; i < targets.Count; i++)
            _ = PlayTarget(targets[i]);   // the satellites tag along with the body: nobody awaits them

        Tween bodyTween = PlayTarget(targets[0]);
        if (bodyTween.isAlive) await bodyTween;
    }

    /// <summary>
    /// Raises every spec of a stage and registers the persistent effects' handles with the session.
    /// A stage missing from the dictionary is a silent no-op: that is the intended configuration, since
    /// not every phase has a VFX for every moment.
    /// </summary>
    protected void RaiseVfxStage(
        LifecycleVfxStage stage, in LifecycleAnimationContext ctx, LifecycleVfxSession session)
    {
        if (_vfx == null || !_vfx.TryGetValue(stage, out List<LifecycleVfxSpec> specs) || specs == null)
            return;

        for (int i = 0; i < specs.Count; i++)
        {
            LifecycleVfxSpec spec = specs[i];
            if (spec == null || !spec.IsConfigured) continue;

            session?.Track(spec.Raise(_vfxChannel, in ctx));
        }
    }

    private bool HasLoopingSpec()
    {
        if (_vfx == null) return false;

        foreach (KeyValuePair<LifecycleVfxStage, List<LifecycleVfxSpec>> entry in _vfx)
        {
            List<LifecycleVfxSpec> specs = entry.Value;
            if (specs == null) continue;

            for (int i = 0; i < specs.Count; i++)
                if (specs[i] != null && specs[i].IsConfigured && specs[i].Loop) return true;
        }

        return false;
    }
}
