using UnityEngine;

[CreateAssetMenu(fileName = "Offensive Equipment Stats", menuName = "Equipment/Offensive Stats")]
public class OffensiveEquipmentStatsSO : EquipmentStatsSO, IOffensiveEquipmentStats
{
    [SerializeField] [Range(0, 100)] private int _critRate;
    [SerializeField] [Range(0, 400)] private int _critDMG = 50;

    public override EquipmentType EquipmentType => EquipmentType.Offensive;
    public int CritRate => _critRate;
    public int CritDMG => _critDMG;
}
