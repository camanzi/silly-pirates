/// <summary>
/// An ability's intent towards its targets. It is not a serialized flag: an ability declares it by
/// implementing <see cref="IOffensiveAbility"/> or <see cref="IDefensiveAbility"/>, so no asset has to be
/// re-wired and a new ability cannot forget to configure it in the Inspector.
///
/// <see cref="Utility"/> is the implicit default for anything implementing neither: movement, spawning,
/// abilities that touch neither the health nor the state of a target.
/// </summary>
public enum AbilityIntent
{
    Utility,
    Offensive,
    Defensive
}
