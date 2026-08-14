/// <summary>
/// Esito di un combattimento. <see cref="None"/> significa "ancora in corso" (vedi
/// <see cref="CombatOutcomeStateSO.IsCombatOver"/>): non è un "nessuno dei due" a fine partita, è
/// lo stato di default finché nessuno ha chiamato <see cref="CombatOutcomeStateSO.Resolve"/>.
/// </summary>
public enum CombatOutcome
{
    None,
    Victory,
    Defeat
}
