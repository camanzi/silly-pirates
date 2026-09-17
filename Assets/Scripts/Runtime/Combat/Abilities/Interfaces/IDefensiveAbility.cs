/// <summary>
/// An ability that benefits its target: healing, revive, buffs, reinforcing oneself. A pure marker — unlike
/// <see cref="IOffensiveAbility"/> it carries no per-category data today.
/// </summary>
public interface IDefensiveAbility : IAbilityIntent
{
    AbilityIntent IAbilityIntent.Intent => AbilityIntent.Defensive;
}
