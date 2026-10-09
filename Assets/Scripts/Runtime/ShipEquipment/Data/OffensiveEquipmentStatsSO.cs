using UnityEngine;

[CreateAssetMenu(fileName = "Offensive Equipment Stats", menuName = "Equipment/Offensive Stats")]
public class OffensiveEquipmentStatsSO : EquipmentStatsSO
{
    public override EquipmentType EquipmentType => EquipmentType.Offensive;
}
