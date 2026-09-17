using System.Collections.Generic;
using PrimeTween;

/// <summary>
/// The contract for a <see cref="LifecycleAnimationSO"/> that does not end on its own: it runs until
/// somebody stops it explicitly (<see cref="CharacterLifecycleAnimator.StopLoop"/>). It adds no data —
/// subclasses stay exactly what they already are, just <see cref="LifecycleAnimationSO.PrepareTarget"/> and
/// <see cref="LifecycleAnimationSO.PlayTarget"/> — it only adds the start track that bypasses
/// <see cref="LifecycleAnimationSO.PlayAsync"/>.
///
/// The bypass is structural, not stylistic: a <see cref="PlayTarget"/> with <c>cycles: -1</c> never
/// returns from an <c>await</c>. The base <c>PlayAsync</c> must NEVER be called on an instance of this
/// type — the guard preventing it sits in <see cref="CharacterLifecycleAnimator.PlayAsync"/> and not here,
/// because it is the animator (and not the SO) that decides which track to use.
/// </summary>
public abstract class LoopingLifecycleAnimationSO : LifecycleAnimationSO
{
    /// <summary>
    /// Starts the loop on ALL targets without awaiting any of them: the non-awaiting twin of
    /// <c>PlayAllTargetsAsync</c>. An infinite tween has no "carrying" tween to wait for, so the
    /// body/satellite distinction of the transition track is not needed here.
    /// </summary>
    public void StartLoops(in LifecycleAnimationContext ctx, LifecycleTweenSession session)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        for (int i = 0; i < targets.Count; i++)
            session.Track(PlayTarget(targets[i]));
    }

    /// <summary>Starts the loop on a single target: used for a satellite registering after the loop has already started.</summary>
    public Tween StartLoopOn(in LifecycleAnimationTarget target) => PlayTarget(target);

    /// <summary>
    /// Raises the <see cref="LifecycleVfxStage.Movement"/> specs. A public wrapper because
    /// <c>RaiseVfxStage</c> is <c>protected</c> on the base (the VFX dictionary drawer needs it, see the
    /// comment there) and the animator has to be able to call it from the loop track exactly as
    /// <see cref="LifecycleAnimationSO.PlayAsync"/> does for the transition one.
    /// </summary>
    public void RaiseMovementVfx(in LifecycleAnimationContext ctx, LifecycleVfxSession session)
        => RaiseVfxStage(LifecycleVfxStage.Movement, in ctx, session);

    /// <summary>
    /// Raises the <see cref="LifecycleVfxStage.End"/> specs. For an animation that does not end on its
    /// own, "once the movement is over" is meaningless: here it means "the moment the loop is stopped".
    /// </summary>
    public void RaiseEndVfx(in LifecycleAnimationContext ctx, LifecycleVfxSession session)
        => RaiseVfxStage(LifecycleVfxStage.End, in ctx, session);
}
