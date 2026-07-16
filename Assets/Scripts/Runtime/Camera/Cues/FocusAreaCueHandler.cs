using UnityEngine;

/// <summary>
/// Frames the ground-targeted area alone (AffectedCells centroid / TargetPoint), excluding the
/// caster. Used for live per-impact reveals where the shot should read as "this is what got hit"
/// rather than "the caster did this".
/// </summary>
public class FocusAreaCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.TryAddGroundAnchor();

        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
