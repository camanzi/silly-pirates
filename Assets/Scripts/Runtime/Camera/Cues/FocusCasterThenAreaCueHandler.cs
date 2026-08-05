using UnityEngine;

/// <summary>
/// Two-beat shot: holds on the caster first (windup), then re-frames onto the ground-targeted areas —
/// one group member per affected point — deliberately ignoring resolved targets even when present, so
/// an AoE cast on an occupied cell always reads as "this zone got hit" rather than "this unit got hit".
/// Area counterpart of FocusCasterThenTargetCueHandler: that one aims to frame characters, this one
/// aims to frame grid zones. Reuses PreShotHold as the caster-hold duration, mirroring
/// FocusCasterThenTargetCueHandler; the final beat has no trailing hold, for the same reason documented
/// there (avoid applying PreShotHold twice per cue).
/// </summary>
public class FocusCasterThenAreaCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);

        context.ClearGroup();
        context.AddGroundAnchors();
        await context.WaitForBlendSettle();
    }
}
