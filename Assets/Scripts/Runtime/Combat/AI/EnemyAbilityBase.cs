public abstract class EnemyAbilityBase : AbilityBase
{
    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
        => AbilityPreviewData.Empty;

    /// <summary>
    /// Evaluates how desirable this ability is in the current context.
    /// Returns float.NegativeInfinity if not applicable or cannot execute.
    /// Populates 'targeting' with the chosen target and data.
    /// </summary>
    public abstract float Score(AIContext context, out TargetingData targeting);
}
