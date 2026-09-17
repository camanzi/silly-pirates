using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Runs a squash&amp;stretch jump sequence on a character's visual pivots (typically the MeshHolder plus
/// its satellites, e.g. the parts of a composite enemy), never on the grid transform: colliders and
/// occupancy stay put.
///
/// Every target moves together over the same duration; only the body's tween (<c>targets[0]</c>) is
/// awaited, and the others tag along — so a sceptre and a hat jump with their owner instead of staying on
/// the ground.
///
/// Split into two halves (<see cref="JumpUpAsync"/> / <see cref="FallDownAsync"/>) rather than being a
/// single method with a callback, so the caller can slot any action in — firing a projectile, raising a
/// camera cue — while the character hangs at the apex, with a plain sequential await and no indirection
/// through delegates.
/// </summary>
public static class JumpSquashStretchHelper
{
    /// <summary>
    /// Anticipation, take-off (with a splash) and the rise up to the apex, with a final settle.
    /// When it returns the pivots sit still at apex position, ready for the caller's hang time.
    /// </summary>
    public static async Awaitable JumpUpAsync(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        float apexWorldHeight,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        // The height arrives in world units (derived from the target's Y), but the tweens move a
        // localPosition: if the parent is scaled the two do not coincide. The satellites share the body's
        // parent, so the factor is the same for all of them.
        Transform bodyPivot = targets[0].Pivot;
        float parentScaleY = bodyPivot.parent != null ? bodyPivot.parent.lossyScale.y : 1f;
        float apexLocalHeight = Mathf.Approximately(parentScaleY, 0f) ? apexWorldHeight : apexWorldHeight / parentScaleY;

        // 1) Anticipation: it squashes while decelerating, "loading" the energy before the burst.
        await ScaleAll(targets, config.AnticipationScale, config.AnticipationDuration, config.AnticipationEase);
        token.ThrowIfCancellationRequested();

        // 2) Take-off: a water splash + a sharp vertical stretch, in parallel with the rise.
        //    The rise decelerates (RiseEase) like a body losing speed under gravity.
        SpawnSplash(config, splashWorldPosition);
        _ = ScaleAll(targets, config.LaunchScale, config.LaunchDuration, config.LaunchEase);
        await MoveAll(targets, apexLocalHeight, config.RiseDuration, config.RiseEase);
        token.ThrowIfCancellationRequested();

        // 3) Apex: the stretch is almost entirely reabsorbed, otherwise while suspended it would read as
        //    rubbery rather than "poised".
        await ScaleAll(targets, config.ApexScale, config.ApexSettleDuration, Ease.OutQuad);
    }

    /// <summary>
    /// The fall from the apex, the impact with its splash and the elastic recovery. It ALWAYS restores the
    /// rest position and scale, exceptions and cancellation included.
    /// </summary>
    public static async Awaitable FallDownAsync(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        try
        {
            // 4) Fall: it accelerates (FallEase), with a slight residual stretch on the way down.
            _ = ScaleAll(targets, config.FallScale, config.FallDuration * 0.6f, Ease.InQuad);
            await MoveAll(targets, 0f, config.FallDuration, config.FallEase);
            token.ThrowIfCancellationRequested();

            // 5) Impact with the water: a splash + a squash, more pronounced than the anticipation
            //    because there is more energy to dissipate on arrival than was stored at the start.
            SpawnSplash(config, splashWorldPosition);
            await ScaleAll(targets, config.LandingScale, config.LandingSquashDuration, config.LandingSquashEase);
            token.ThrowIfCancellationRequested();

            // 6) Elastic follow-through: it makes the impact read as absorbed rather than cut short.
            await ScaleAllToRest(targets, config.LandingRecoveryDuration, config.LandingRecoveryEase);
        }
        finally
        {
            ResetToRest(targets);
        }
    }

    /// <summary>
    /// Returns the pivots to their rest pose. Safe on already destroyed objects (Unity's null comparison
    /// catches them). The caller should call it too, to cover failures that happen before
    /// <see cref="FallDownAsync"/>.
    /// </summary>
    public static void ResetToRest(IReadOnlyList<LifecycleAnimationTarget> targets)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Count; i++)
            targets[i].ResetPoseToRest();
    }

    /// <summary>
    /// Starts the scaling on every target and returns the body's, the only one worth awaiting:
    /// same duration for all of them, so they finish together.
    /// </summary>
    private static Tween ScaleAll(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        JumpAnimationConfigSO.SquashStretchScale scale,
        float duration,
        Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.Scale(target.Pivot, ScaleOf(target.RestLocalScale, scale), duration, ease);
        }

        var body = targets[0];
        return Tween.Scale(body.Pivot, ScaleOf(body.RestLocalScale, scale), duration, ease);
    }

    private static Tween ScaleAllToRest(
        IReadOnlyList<LifecycleAnimationTarget> targets, float duration, Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.Scale(target.Pivot, target.RestLocalScale, duration, ease);
        }

        var body = targets[0];
        return Tween.Scale(body.Pivot, body.RestLocalScale, duration, ease);
    }

    /// <summary>Raises every target by <paramref name="localHeight"/> above its own rest pose.</summary>
    private static Tween MoveAll(
        IReadOnlyList<LifecycleAnimationTarget> targets, float localHeight, float duration, Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.LocalPosition(
                target.Pivot, target.RestLocalPosition + Vector3.up * localHeight, duration, ease);
        }

        var body = targets[0];
        return Tween.LocalPosition(
            body.Pivot, body.RestLocalPosition + Vector3.up * localHeight, duration, ease);
    }

    private static Vector3 ScaleOf(Vector3 restScale, JumpAnimationConfigSO.SquashStretchScale s)
        => new(restScale.x * s.Horizontal, restScale.y * s.Vertical, restScale.z * s.Horizontal);

    private static void SpawnSplash(JumpAnimationConfigSO config, Vector3 worldPosition)
    {
        // splash not configured: that is a choice, not an error
        if (config.SplashVfxPrefab == null || config.VfxChannel == null) return;
        config.VfxChannel.RaiseEvent(VfxCue.At(config.SplashVfxPrefab, worldPosition));
    }
}
