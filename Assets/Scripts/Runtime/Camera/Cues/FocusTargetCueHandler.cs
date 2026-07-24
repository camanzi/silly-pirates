using UnityEngine;

/// <summary>
/// Frames the resolved targets alone (falls back to the ground anchor if none are live). Used for the
/// payoff beat of an ability whose windup already framed the caster, e.g. a mid-command cue raised
/// after the caster's animation finishes to reveal what the effect lands on.
/// </summary>
public class FocusTargetCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        if (!context.AddTargets())
            context.TryAddGroundAnchor();

        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
