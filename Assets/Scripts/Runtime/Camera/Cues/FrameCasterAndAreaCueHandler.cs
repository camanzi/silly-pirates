using UnityEngine;

/// <summary>
/// Frames the caster and the ground-targeted area (AffectedCells centroid / TargetPoint), ignoring
/// resolved targets entirely so the shot always reads as "the blast zone" rather than individual units.
/// </summary>
public class FrameCasterAndAreaCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        context.TryAddGroundAnchor();

        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
