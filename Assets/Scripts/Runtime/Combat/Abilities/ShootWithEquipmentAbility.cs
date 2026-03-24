using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Equipment/Abilities/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    public override bool CanExecute(GridElement caster, Vector3Int target)
    {
        throw new System.NotImplementedException();
    }

    public override ICommand CreateCommand(GridElement caster, Vector3Int targetCell, List<GridElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(Vector3Int targetCell, GridElement caster)
    {
        throw new System.NotImplementedException();
    }
}
