using UnityEngine;

/// <summary>
/// Frames the caster and every resolved target in one static group shot for the whole cast.
/// Falls back to the ground anchor (AffectedCells centroid / TargetPoint) when no targets resolve
/// (e.g. ground-targeted abilities without an ITargettable).
/// </summary>
public class FramePairCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        if (!context.AddTargets())
            context.TryAddGroundAnchor();

        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
