using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    public override bool CanExecute(GridElement caster, Vector3 target)
    {
        return false;
    }

    public override ICommand CreateCommand(GridElement caster, Vector3 target, List<GridElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(Vector3 target, GridElement caster)
    {
        // Debug.DrawRay(caster.worldPosition, target, Color.green, 0.5f);

        List<Vector3> targetObjs = new() { target };
        return new AbilityPreviewData(affectedCells: new(), freeAimTargets: targetObjs);
    
    }
}
