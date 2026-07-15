using UnityEngine;

/// <summary>
/// Two-beat shot: holds on the caster first (windup), then re-frames onto the resolved targets
/// (or the ground anchor fallback) for the payoff. Reuses PreShotHold as the caster-hold duration
/// so no CameraCueProfileSO schema change is needed; the final beat has no trailing hold to avoid
/// applying PreShotHold twice per cue.
/// </summary>
public class FocusCasterThenTargetCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);

        context.ClearGroup();
        if (!context.AddTargets())
            context.TryAddGroundAnchor();
        await context.WaitForBlendSettle();
    }
}
