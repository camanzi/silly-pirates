public interface IOffensiveEquipmentStats
{
    int BaseAccuracy { get; }
    int GetOvercapAccuracyBonus(int extraPoints);
}
