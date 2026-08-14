/// <summary>
/// Query di sola lettura sulla coda dei turni. Centralizza in un unico punto lo stesso loop
/// "conta i vivi per squadra" che oggi è replicato a mano in diverse ability nemiche
/// (SlimyBallAbility, SlimyCurseAbility, SuperSlimyBallAbility, HealingWaterAbility,
/// SpeedBoostAbility, HighestHpTargetSelectionSO). Quelle non vengono toccate qui: sono AI già
/// funzionante e non c'è motivo di rifattorizzarle per aggiungere questa feature. L'helper nasce
/// comunque nella forma giusta per essere il punto d'appoggio di eventuali refactor futuri.
/// </summary>
public static class TurnOrderQueries
{
    // Nemico = agente della coda che è effettivamente un HostileCharacter; qualunque altro
    // ITurnAgent (GridCharacter e derivati) è per definizione un giocante. Lo stesso identico
    // filtro usato nelle ability AI citate sopra.
    public static int CountAliveEnemies(this TurnOrderDataSO turnOrder)
    {
        if (turnOrder == null) return 0;

        int count = 0;
        foreach (var state in turnOrder.TurnQueue)
        {
            if (state.Agent is not HostileCharacter) continue;
            if (state.Agent is not IHealthOwner ho || ho.Health == null || !ho.Health.IsAlive) continue;
            count++;
        }
        return count;
    }

    public static int CountAlivePlayers(this TurnOrderDataSO turnOrder)
    {
        if (turnOrder == null) return 0;

        int count = 0;
        foreach (var state in turnOrder.TurnQueue)
        {
            if (state.Agent is HostileCharacter) continue;
            if (state.Agent is not IHealthOwner ho || ho.Health == null || !ho.Health.IsAlive) continue;
            count++;
        }
        return count;
    }
}
