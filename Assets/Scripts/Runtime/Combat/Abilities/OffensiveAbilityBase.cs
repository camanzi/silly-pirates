public abstract class OffensiveAbilityBase : AbilityBase
{
    public override float? GetHitChance(IInteractableElement caster, TargetingData targetingData)
    {
        float accuracy = (caster as IAccuracyOwner)?.EffectiveAccuracy ?? 0;
        float evasion = targetingData.selectedTarget?.EffectiveEvasion ?? 0;
        return MathUtils.CalculateHitChance(accuracy, evasion);
    }
}
