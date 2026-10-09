using UnityEngine;

/// <summary>
/// Frames everything the cue carries in one wide static shot: the caster (when there is one), every
/// resolved target and one member per affected point. Meant for "whole fight" shots such as the ship's
/// Broadside, where the affected points mark fixed extents (bow and stern) that must stay on screen
/// next to the targets.
/// </summary>
public class FrameAllCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        context.AddTargets();
        context.AddGroundAnchors();

        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
