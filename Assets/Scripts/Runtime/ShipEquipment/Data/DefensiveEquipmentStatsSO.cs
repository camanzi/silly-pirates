using UnityEngine;

[CreateAssetMenu(fileName = "Defensive Equipment Stats", menuName = "Equipment/Defensive Stats")]
public class DefensiveEquipmentStatsSO : EquipmentStatsSO
{
    public override EquipmentType EquipmentType => EquipmentType.Defensive;
}
