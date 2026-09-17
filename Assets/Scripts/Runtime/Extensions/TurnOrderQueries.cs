/// <summary>
/// Read-only queries over the turn queue. It centralizes in one place the same "count the living per
/// team" loop that is currently hand-rolled in several enemy abilities (SlimyBallAbility,
/// SlimyCurseAbility, SuperSlimyBallAbility, HealingWaterAbility, SpeedBoostAbility,
/// HighestHpTargetSelectionSO). Those are deliberately left alone here: they are working AI and there is
/// no reason to refactor them just to add this feature. The helper is nevertheless written in the right
/// shape to serve as the foothold for any future refactor.
/// </summary>
public static class TurnOrderQueries
{
    // An enemy is a queue agent that actually is a HostileCharacter; any other ITurnAgent (GridCharacter
    // and its derivatives) is by definition a player character. Exactly the same filter used in the AI
    // abilities cited above.
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
