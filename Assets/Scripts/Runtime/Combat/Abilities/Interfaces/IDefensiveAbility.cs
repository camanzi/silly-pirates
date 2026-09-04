/// <summary>
/// Abilita' a beneficio del bersaglio: cura, revive, buff, rinforzo di se stessi. Marker puro — a
/// differenza di <see cref="IOffensiveAbility"/> non ha oggi nessun dato per-categoria da portare.
/// </summary>
public interface IDefensiveAbility : IAbilityIntent
{
    AbilityIntent IAbilityIntent.Intent => AbilityIntent.Defensive;
}
