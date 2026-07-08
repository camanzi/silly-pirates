using UnityEngine;

[CreateAssetMenu(fileName = "Offensive Equipment Stats", menuName = "Equipment/Offensive Stats")]
public class OffensiveEquipmentStatsSO : EquipmentStatsSO, IOffensiveEquipmentStats
{
    [SerializeField] private int _baseAccuracy = 100;
    [SerializeField] private int _accuracyPerOvercap = 10;
    [SerializeField] private AccuracyOvercapPassiveSO _overcapPassiveTemplate;

    public override EquipmentType EquipmentType => EquipmentType.Offensive;
    public int BaseAccuracy => _baseAccuracy;
    public int GetOvercapAccuracyBonus(int extraPoints) => extraPoints * _accuracyPerOvercap;

    public override PassiveAbilitySO OvercapPassiveTemplate => _overcapPassiveTemplate;
    public override int GetOvercapBonus(int extraPoints) => GetOvercapAccuracyBonus(extraPoints);
}
