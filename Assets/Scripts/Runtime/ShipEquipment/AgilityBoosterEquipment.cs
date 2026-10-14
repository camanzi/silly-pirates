/// <summary>
/// Utility equipment that speeds the ship up. The crew only awakens it: from then on its agility bonus counts
/// towards <see cref="ShipTurnAgent.EffectiveAgility"/>, so the ship moves up the turn order at once. It then
/// stays awake and idle until the ship has acted, and only then goes on cooldown — which also removes the
/// bonus, so the ship's following round is back at its base pace.
/// </summary>
public class AgilityBoosterEquipment : UtilityEquipment, IShipTurnParticipant, IAutoActivatedEquipment, IAgilityModifier
{
    private AgilityBoosterStatsSO Stats => StatsConfig as AgilityBoosterStatsSO;

    public int GetFlatAgilityBonus() => Stats != null ? Stats.FlatAgilityBonus : 0;
    public float GetPercentageAgilityBonus() => Stats != null ? Stats.PercentageAgilityBonus : 0f;

    public void OnShipTurnEnded()
    {
        if (!IsAwake) return;

        // Same closing as ShootCommand: the points are spent and the cooldown starts. Leaving the active state
        // takes the booster out of the ShipTurnRosterSO, and with it its bonus.
        ConsumeAllAwakeningPoints();
        Cooldown = Stats != null ? Stats.CooldownTurns : 1;
    }
}
