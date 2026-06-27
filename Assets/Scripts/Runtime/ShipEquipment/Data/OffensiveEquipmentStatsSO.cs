using UnityEngine;

[CreateAssetMenu(fileName = "Offensive Equipment Stats", menuName = "Equipment/Offensive Stats")]
public class OffensiveEquipmentStatsSO : EquipmentStatsSO, IOffensiveEquipmentStats
{
    [SerializeField] [Range(0, 100)] private int _baseHitPercentage = 100;

    public override EquipmentType EquipmentType => EquipmentType.Offensive;
    public int BaseHitPercentage => _baseHitPercentage;
}
