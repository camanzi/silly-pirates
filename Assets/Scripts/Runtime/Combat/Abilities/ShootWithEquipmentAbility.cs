using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    public override bool CanExecute(IInteractableElement caster, Vector3 target)
    {
        return false;
    }

    public override ICommand CreateCommand(IInteractableElement caster, Vector3 target, List<IInteractableElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(Vector3 target, IInteractableElement caster)
    {
        List<Vector3> targetObjs = new() { target };
        return new AbilityPreviewData(affectedCells: new(), freeAimTargets: targetObjs);
    
    }
}
