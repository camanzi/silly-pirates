using UnityEngine;

/// <summary>
/// Frames the caster alone. Used for heals/auras/summons whose payoff happens at the caster's feet.
/// </summary>
public class FocusCasterCueHandler : ICameraCueHandler
{
    public async Awaitable RunAsync(CameraCueContext context)
    {
        context.ClearGroup();
        context.AddCaster();
        await context.WaitForBlendSettle();
        await context.Hold(context.Profile.PreShotHold);
    }
}
