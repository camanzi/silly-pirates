/// <summary>
/// The outcome of a combat. <see cref="None"/> means "still in progress" (see
/// <see cref="CombatOutcomeStateSO.IsCombatOver"/>): it is not a "neither of the two" at the end of a
/// match, it is the default state until somebody calls <see cref="CombatOutcomeStateSO.Resolve"/>.
/// </summary>
public enum CombatOutcome
{
    None,
    Victory,
    Defeat
}
