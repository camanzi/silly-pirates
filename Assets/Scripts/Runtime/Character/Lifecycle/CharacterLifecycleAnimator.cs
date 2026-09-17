using System;
using System.Collections.Generic;
using System.Threading;
using AYellowpaper.SerializedCollections;
using UnityEngine;

/// <summary>
/// Plays lifecycle animations (spawn, death, and so on) on a character, agnostic to whether it is a
/// <see cref="HostileCharacter"/> or a <see cref="GridCharacter"/>. The <see cref="LifecycleAnimationSO"/>
/// assets it references are stateless and shared: no per-instance Instantiate.
///
/// Besides the body pivot, the animator handles "satellites" registered by third parties
/// (<see cref="RegisterSatellite"/>): objects that visually belong to the character but live outside the
/// pivot's hierarchy — the parts of a composite enemy, for instance — and would therefore inherit
/// nothing from the body's animations.
/// </summary>
public class CharacterLifecycleAnimator : MonoBehaviour
{
    [SerializeField] private Transform _animationRoot;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SerializedDictionary<LifecyclePhase, LifecycleAnimationSO> _animations;

    public bool IsPlaying { get; private set; }

    /// <summary>Raised after a phase's <c>Prepare</c>, both in <see cref="PrepareHidden"/> and in <see cref="PlayAsync"/>.</summary>
    public event Action<LifecyclePhase> OnPhaseStarted;

    /// <summary>Raised only on actual completion (never on cancellation).</summary>
    public event Action<LifecyclePhase> OnPhaseCompleted;

    /// <summary>The prepared or running phase; <c>null</c> when the character is at rest.</summary>
    public LifecyclePhase? ActivePhase => _activeAnimation != null ? _activePhase : null;

    /// <summary>
    /// A track independent of <see cref="ActivePhase"/>/<see cref="IsPlaying"/>: a transition (Spawn,
    /// Leave) and a loop (Idle, and tomorrow PostDeath) are never active together, but no existing
    /// consumer should ever notice the loop, so the two states stay separate rather than being unified.
    /// </summary>
    public bool IsLoopingAnimationPlaying => _loopingAnimation != null;

    /// <summary>The active looping phase; <c>null</c> when the character is at rest on this track.</summary>
    public LifecyclePhase? LoopingPhase => _loopingAnimation != null ? _loopingPhase : null;

    /// <summary>
    /// The visual pivot (e.g. MeshHolder) for one-off animations outside the <see cref="LifecyclePhase"/>
    /// set, such as combat jumps. If the prefab has no dedicated pivot, <see cref="EnsureRestPoseCaptured"/>
    /// has already fallen back to <c>transform</c> and logged an error: callers must read
    /// "<c>AnimationRoot == transform</c>" as "no safe pivot available" and must NOT animate it, or they
    /// will move the collider and the grid position along with it.
    /// </summary>
    public Transform AnimationRoot
    {
        get
        {
            EnsureRestPoseCaptured();
            return _animationRoot;
        }
    }

    /// <summary>
    /// Every visual target: <c>[0]</c> is the body, the rest are the registered satellites. Exposed
    /// because animations outside the <see cref="LifecyclePhase"/> set (the jumps) have to move the body
    /// TOGETHER with its satellites, or the parts would stay on the ground while the body leaps.
    /// </summary>
    public IReadOnlyList<LifecycleAnimationTarget> Targets
    {
        get
        {
            EnsureRestPoseCaptured();
            return _targets;
        }
    }

    private readonly List<LifecycleAnimationTarget> _targets = new();

    // Per-instance and not on the SO (which is shared between prefabs): it holds the handles of the
    // running phase's persistent VFX. Reused phase after phase, so repeated spawns allocate nothing.
    private readonly LifecycleVfxSession _vfxSession = new();

    // The loop track, separate from the fields above and deliberately not reused: _vfxSession.StopAll()
    // runs on every BeginPhase (behind the "resuming" guard), so sharing it would kill the loop's VFX on
    // every Spawn and entangle the two tracks the design wants independent.
    private readonly LifecycleTweenSession _loopTweenSession = new();
    private readonly LifecycleVfxSession _loopVfxSession = new();

    private LifecycleAnimationContext _context;
    private bool _restPoseCaptured;

    // true from Start onwards. Before that the scene is still initializing: raising a cue means talking
    // to a director that may not have run Awake yet, or to a listener that has not subscribed yet — and
    // in that second case the cue is lost silently, with no error at all. The visual state, by contrast,
    // is applied right away: Prepare talks to nobody.
    private bool _initialized;
    private bool _pendingVfxBegin;

    // Non-null only between the start of a phase and its completion: it is what lets a late-registering
    // satellite hook into the phase already in flight.
    private LifecycleAnimationSO _activeAnimation;
    private LifecyclePhase _activePhase;

    // A mirror of the three fields above, but for the loop track. _pendingLoopStart replicates the same
    // initialization window as _pendingVfxBegin: StartLoop can be called from OnEnable, before Start.
    private LoopingLifecycleAnimationSO _loopingAnimation;
    private LifecyclePhase _loopingPhase;
    private bool _pendingLoopStart;

    // A bool and not a counter: the project's three suspension points (jump, telegraph shake, sceptre
    // orbit) are never concurrent on the same character, and StopLoop() clears it on every transition
    // anyway — no refcount is needed for a state that is already guaranteed not to nest.
    private bool _loopSuspended;

    private void Awake() => EnsureRestPoseCaptured();

    /// <summary>
    /// The end of the initialization window: the only moment guaranteed to be after ALL of the scene's
    /// Awake and OnEnable calls, and still within the same frame in which the character was activated,
    /// before it is rendered — so a VFX deferred to here is never seen starting late.
    /// </summary>
    private void Start()
    {
        _initialized = true;
        FlushPendingVfxBegin();
        FlushPendingLoopStart();

        // Covers the character with no Spawn slot configured: Play(Spawn) runs in OnEnable and sets
        // _activeAnimation synchronously (see the comment on OnPhaseCompleted), so if by this point there
        // is neither a transition nor a loop already running, nothing will ever start one on its own.
        if (_activeAnimation == null && _loopingAnimation == null)
            StartLoop(LifecyclePhase.Idle);
    }

    /// <summary>Raises the <see cref="LifecycleVfxStage.Prepare"/> VFX deferred by <see cref="BeginPhase"/>.</summary>
    private void FlushPendingVfxBegin()
    {
        if (!_pendingVfxBegin) return;

        _pendingVfxBegin = false;
        _activeAnimation?.BeginVfx(_vfxSession, in _context);
    }

    /// <summary>Starts the loop deferred by <see cref="StartLoop"/> when it was called before <see cref="Start"/>.</summary>
    private void FlushPendingLoopStart()
    {
        if (!_pendingLoopStart) return;

        _pendingLoopStart = false;
        if (_loopingAnimation != null) BeginLoopPlayback(_loopingAnimation);
    }

    /// <summary>
    /// A safety net for the phase that was prepared and never played: <see cref="PrepareHidden"/> opens
    /// the VFX session, and if the character is destroyed or disabled during the intro no <c>finally</c>
    /// in <see cref="PlayAsync"/> would ever close it. The last-resort fallback is still
    /// <c>VfxDirector.OnDisable</c> → <c>StopEverything()</c>, but it must not be the only one.
    /// </summary>
    private void OnDisable()
    {
        _vfxSession.StopAll();
        _pendingVfxBegin = false;   // disabled before Start: on waking up it must not light anything
        StopLoop();
    }

    /// <summary>
    /// Captures the rest pose exactly once, resolving any missing references first.
    /// Lazy and idempotent because on <c>Object.Instantiate</c> another component's <c>OnEnable</c>
    /// (<see cref="HostileCharacter.OnCombatJoin"/> → <see cref="Play"/>) can run before this
    /// <c>Awake</c>: without the lazy capture the context would stay <c>default</c> and the animation
    /// would be skipped silently. The flag guarantees the pose is never recaptured after an animation
    /// has moved it — a requirement of <see cref="ResetToRest"/>.
    /// </summary>
    private void EnsureRestPoseCaptured()
    {
        if (_restPoseCaptured) return;
        _restPoseCaptured = true;

        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_animationRoot == null)
        {
            _animationRoot = transform;
            Debug.LogError(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': _animationRoot not assigned. " +
                "The animations will move the grid transform (colliders included) instead of just the " +
                "visual pivot. Assign a child pivot (e.g. MeshHolder).", this);
        }

        // The body always occupies index 0; satellites queue up after it.
        _targets.Add(LifecycleAnimationTarget.Capture(_animationRoot, _spriteRenderer));
        _context = new LifecycleAnimationContext(transform, _targets);
    }

    /// <summary>
    /// Adds a satellite to the character's animations. The rest pose is captured NOW: registering an
    /// object somebody else has already moved would record a false rest pose for it.
    ///
    /// Late registration: the ordering between the registrant's <c>Awake</c> and the <c>OnEnable</c> that
    /// starts the spawn is not guaranteed, so if a phase has already been prepared the satellite catches
    /// up with it at once — otherwise it would sit visible on the surface while the body is underwater.
    /// </summary>
    public void RegisterSatellite(Transform pivot, SpriteRenderer renderer = null)
    {
        if (pivot == null) return;
        EnsureRestPoseCaptured();

        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i].Pivot == pivot) return;

        if (renderer == null) renderer = pivot.GetComponentInChildren<SpriteRenderer>();

        var target = LifecycleAnimationTarget.Capture(pivot, renderer);
        _targets.Add(target);

        // The two tracks are mutually exclusive (BeginPhase always stops the loop before a transition),
        // but a satellite can register while either one is running: the same catch-up applies to both, or
        // it would lag behind until the running phase or loop starts over.
        if (_activeAnimation != null)
        {
            _activeAnimation.PrepareTarget(target);
            if (IsPlaying) _ = _activeAnimation.PlayTarget(target);
        }

        if (_loopingAnimation != null)
        {
            _loopingAnimation.PrepareTarget(target);
            if (!_loopSuspended) _loopTweenSession.Track(_loopingAnimation.StartLoopOn(target));
        }
    }

    public void UnregisterSatellite(Transform pivot)
    {
        if (pivot == null) return;

        // Start from 1: index 0 is the body and cannot be removed.
        for (int i = 1; i < _targets.Count; i++)
        {
            if (_targets[i].Pivot != pivot) continue;
            _targets.RemoveAt(i);
            return;
        }
    }

    /// <summary>Fire-and-forget: starts the animation without awaiting its completion.</summary>
    public async void Play(LifecyclePhase phase)
    {
        try
        {
            await PlayAsync(phase, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Destroyed during the animation: nothing to do.
        }
    }

    /// <summary>
    /// Instantly applies a phase's "hidden" state (e.g. pivot underwater, alpha 0) without playing the
    /// animation. Used by the combat intro sequence to suppress the automatic spawn of
    /// <see cref="HostileCharacter.OnCombatJoin"/> until the director decides to replay it with
    /// <see cref="PlayAsync"/>. Same SO resolution as <see cref="PlayAsync"/> and the same log messages:
    /// the SO stays stateless, all mutable state lives in the context.
    /// </summary>
    public void PrepareHidden(LifecyclePhase phase)
    {
        EnsureRestPoseCaptured();

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        BeginPhase(phase, animation);
    }

    public async Awaitable PlayAsync(LifecyclePhase phase, CancellationToken token)
    {
        EnsureRestPoseCaptured();

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        // A Play() written by mistake against a looping phase (e.g. Idle) must NEVER end up on
        // `await bodyTween`: that tween never returns, and the await would block both this method and
        // IsPlaying forever. Redirect to the right track instead of crashing or hanging.
        if (animation is LoopingLifecycleAnimationSO)
        {
            StartLoop(phase);
            return;
        }

        BeginPhase(phase, animation);

        IsPlaying = true;
        try
        {
            // IsPlaying is already true just above, synchronously: WaitUntilIdleAsync
            // (SpawnAlliesCommand) must never catch the animator at rest between the Play and the first
            // await.
            //
            // Play() is async void and runs synchronously inside OnEnable: without this wait the Movement
            // stage too would fire at systems that are not initialized yet. Zero cost once the scene is up.
            while (!_initialized)
                await Awaitable.NextFrameAsync(token);

            FlushPendingVfxBegin();

            await animation.PlayAsync(_context, _vfxSession, token);
        }
        finally
        {
            IsPlaying = false;

            // Here and not inside the SO: this covers token cancellation (a character destroyed halfway
            // through emerging), which would otherwise leave the persistent effects on forever.
            _vfxSession.StopAll();
        }

        // Outside the finally: a cancelled phase is not a completed phase.
        _activeAnimation = null;
        OnPhaseCompleted?.Invoke(phase);

        // A single place covering both the normal spawn and the combat intro one (which goes through here
        // anyway), instead of branching on "if this is a spawn, start the idle" in several places.
        if (animation.StartsLoopOnComplete) StartLoop(animation.LoopPhaseOnComplete);
    }

    /// <summary>
    /// Takes ownership of the targets and applies their initial state. Other people's tweens are stopped
    /// first: a telegraph shake or a jump interrupted halfway write to the same <c>localPosition</c> the
    /// lifecycle animation is about to animate, and the two would fight over control of it.
    ///
    /// The <see cref="LifecycleVfxStage.Prepare"/> VFX start here, right after the initial state — but
    /// ONLY on the phase's first preparation. <see cref="PrepareHidden"/> and <see cref="PlayAsync"/> both
    /// come through here for the same phase during the combat intro: without the guard, the bubbles of an
    /// emergence would restart from scratch at the moment the monster begins to rise.
    ///
    /// If the scene is still initializing (this method is reached from the character's OnEnable) the VFX
    /// are queued and raised by <see cref="Start"/>: the visual state cannot wait, the cues towards the
    /// other systems can.
    /// </summary>
    private void BeginPhase(LifecyclePhase phase, LifecycleAnimationSO animation)
    {
        // Up front and not delegated to StopActiveTweens below: that one does kill the loop's tweens
        // (they are on the same Transforms), but not the handles registered in _loopTweenSession, not the
        // loop's VFX and not the suspension flag — StopLoop() is the only one closing all three together.
        StopLoop();

        // A deferred begin counts as an already-open session too: without this, PrepareHidden and
        // PlayAsync on the same phase would queue it twice.
        bool resuming = (_vfxSession.IsOpen || _pendingVfxBegin)
                        && _activeAnimation == animation && _activePhase == phase;

        // A different phase from the prepared one: any persistent effects left open do not belong to it.
        if (!resuming)
        {
            _vfxSession.StopAll();
            _pendingVfxBegin = false;
        }

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].StopActiveTweens();

        _activeAnimation = animation;
        _activePhase = phase;

        animation.Prepare(in _context);

        if (!resuming)
        {
            if (_initialized) animation.BeginVfx(_vfxSession, in _context);
            else _pendingVfxBegin = true;
        }

        OnPhaseStarted?.Invoke(phase);
    }

    private bool TryResolveAnimation(LifecyclePhase phase, out LifecycleAnimationSO animation)
    {
        animation = null;

        // An empty dictionary is never an intended configuration — a prefab with no lifecycle animations
        // does not carry this component at all. In a build it means the serialized data was lost on the
        // clone (see SerializedDictionary.OnAfterDeserialize, which in the player empties the backing list
        // after the first deserialization).
        if (_animations == null || _animations.Count == 0)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': empty animation dictionary, " +
                $"phase {phase} ignored.", this);
            return false;
        }

        // An unconfigured phase: silent, because that is the intended configuration (not every character
        // has an animation for every phase).
        if (!_animations.TryGetValue(phase, out animation))
            return false;

        // Entry present but with no SO: almost certainly forgotten wiring, not a deliberate absence.
        if (animation == null)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': phase {phase} is in the dictionary " +
                "but has no LifecycleAnimationSO assigned.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// A spin-wait on <see cref="IsPlaying"/>: necessary because Unity's Awaitable is single-consumption
    /// and cannot be stashed in a field by <see cref="Play"/> to be awaited elsewhere
    /// (the same pattern as <c>EnemyTurnDriver.ExecuteTurnAsync</c>).
    /// </summary>
    public async Awaitable WaitUntilIdleAsync(CancellationToken token)
    {
        while (IsPlaying)
        {
            token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync(token);
        }
    }

    public void ResetToRest()
    {
        EnsureRestPoseCaptured();

        for (int i = 0; i < _targets.Count; i++)
        {
            _targets[i].StopActiveTweens();
            _targets[i].ResetToRest();
        }

        _vfxSession.StopAll();
        _pendingVfxBegin = false;
        _activeAnimation = null;

        StopLoop();
    }

    /// <summary>
    /// Starts the loop track on the given phase — today only <see cref="LifecyclePhase.Idle"/>, tomorrow
    /// a <c>PostDeath</c> as well: the mechanism does not know, and must not know, which phase runs on top
    /// of it. Same resolution as <see cref="PlayAsync"/> (<see cref="TryResolveAnimation"/>, same logs),
    /// but it NEVER goes through <c>LifecycleAnimationSO.PlayAsync</c>: that track assumes a tween that
    /// ends, and a loop has none.
    /// </summary>
    public void StartLoop(LifecyclePhase phase)
    {
        EnsureRestPoseCaptured();

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        if (animation is not LoopingLifecycleAnimationSO loopingAnimation)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': phase {phase} is not a " +
                $"{nameof(LoopingLifecycleAnimationSO)}, StartLoop ignored.", this);
            return;
        }

        // Closes any previous loop (VFX, tweens, suspension) before taking ownership: the same reason
        // BeginPhase stops other people's tweens before animating the same pivots.
        StopLoop();

        _loopingAnimation = loopingAnimation;
        _loopingPhase = phase;

        loopingAnimation.Prepare(in _context);

        // The same initialization window as BeginPhase/_pendingVfxBegin: a cue raised before Start can
        // talk to a director that is not awake yet, or be lost on a listener that has not subscribed yet.
        if (!_initialized)
        {
            _pendingLoopStart = true;
            return;
        }

        BeginLoopPlayback(loopingAnimation);
    }

    /// <summary>Raises Prepare+Movement and starts the tweens on every target. Shared between <see cref="StartLoop"/> (the synchronous path) and <see cref="FlushPendingLoopStart"/> (the deferred path).</summary>
    private void BeginLoopPlayback(LoopingLifecycleAnimationSO animation)
    {
        animation.BeginVfx(_loopVfxSession, in _context);
        animation.RaiseMovementVfx(in _context, _loopVfxSession);

        _loopTweenSession.Begin();
        animation.StartLoops(in _context, _loopTweenSession);
    }

    /// <summary>
    /// Stops the loop track: VFX (End stage), tweens and the animation registration, restoring only the
    /// targets' position. Called by <see cref="BeginPhase"/>, <see cref="ResetToRest"/> and
    /// <c>OnDisable</c> to put the character back "at rest" with respect to this track.
    ///
    /// It also clears <see cref="_loopSuspended"/>, but that is a stopgap and not a net to rely on: it
    /// only fires when the character changes phase, and whoever suspended typically stays alive and idle
    /// for the whole loan. Every <see cref="SuspendLoop"/> must have its own <see cref="ResumeLoop"/> on a
    /// path that actually runs (cf. <see cref="MultiStepAbilityStepSO.EndPartShake"/>).
    /// </summary>
    public void StopLoop()
    {
        EnsureRestPoseCaptured();

        if (_loopingAnimation == null)
        {
            _loopSuspended = false;
            return;
        }

        _loopingAnimation.RaiseEndVfx(in _context, _loopVfxSession);

        _loopTweenSession.StopAll();
        _loopVfxSession.StopAll();
        RestoreLoopPose();

        _loopingAnimation = null;
        _pendingLoopStart = false;
        _loopSuspended = false;
    }

    /// <summary>
    /// Suspends ONLY the loop's movement: used by the commands that tween the same <c>localPosition</c>
    /// of a registered target (jump, telegraph shake, sceptre orbit) and would otherwise fight over
    /// control of it. The animation and its VFX stay registered — this is not a clash of effects, only of
    /// transforms — so <see cref="ResumeLoop"/> resumes without replaying Prepare/Movement.
    /// </summary>
    public void SuspendLoop()
    {
        EnsureRestPoseCaptured();

        if (_loopingAnimation == null || _loopSuspended) return;

        _loopSuspended = true;
        _loopTweenSession.StopAll();
        RestoreLoopPose();
    }

    /// <summary>
    /// The counterpart of <see cref="SuspendLoop"/>: restarts the tweens on all current targets. There is
    /// no need to single out the ones registered during the suspension: <see cref="RegisterSatellite"/>
    /// already adds them to <c>_targets</c>, and <see cref="LoopingLifecycleAnimationSO.StartLoops"/>
    /// iterates over all of them.
    ///
    /// It restores the pose BEFORE resuming, the twin of what <see cref="SuspendLoop"/> does on the way
    /// out: a suspension is a loan of the transform, and whoever gives it back should not have to know
    /// where rest was. It genuinely matters — PrimeTween's <c>Tween.Stop()</c> is a kill, not a rewind: an
    /// infinite shake stopped mid-oscillation leaves the pivot offset, and the loop's tweens resume from
    /// the current value, freezing that offset in place forever.
    /// </summary>
    public void ResumeLoop()
    {
        if (!_loopSuspended || _loopingAnimation == null) return;

        _loopSuspended = false;
        RestoreLoopPose();
        _loopTweenSession.Begin();
        _loopingAnimation.StartLoops(in _context, _loopTweenSession);
    }

    /// <summary>
    /// Restores only the targets' position — NEVER their scale. Unlike
    /// <see cref="LifecycleAnimationTarget.ResetPoseToRest"/>, which also touches <c>localScale</c>, the
    /// loop does not animate the scale and must not claim it: <see cref="JumpSquashStretchHelper"/> and
    /// the legacy squash-stretch commands are already using it, and a restore here would snap it back to
    /// rest halfway through their tween.
    /// </summary>
    private void RestoreLoopPose()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            Transform pivot = _targets[i].Pivot;
            if (pivot != null) pivot.localPosition = _targets[i].RestLocalPosition;
        }
    }
}
