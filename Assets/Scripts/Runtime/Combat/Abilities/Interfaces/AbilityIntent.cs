/// <summary>
/// Intenzione di un'abilita' verso i suoi bersagli. Non e' un flag serializzato: si dichiara implementando
/// <see cref="IOffensiveAbility"/> o <see cref="IDefensiveAbility"/>, cosi' nessun asset va ri-cablato e una
/// nuova abilita' non puo' dimenticare di configurarla in Inspector.
///
/// <see cref="Utility"/> e' il default implicito di chi non implementa nessuna delle due: movimento,
/// spawn, abilita' che non toccano ne' la vita ne' lo stato di un bersaglio.
/// </summary>
public enum AbilityIntent
{
    Utility,
    Offensive,
    Defensive
}
