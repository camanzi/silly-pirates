using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    [Header("Cannon ability configs")]
    [SerializeField] private int _maxTargets = 1;

    public override bool CanExecute(IInteractableElement caster, TargetingData targetingData)
    {
        return _selectionCtx.CurrentTargets.Count == _maxTargets;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData targetingData, List<IInteractableElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(TargetingData targetingData, IInteractableElement caster)
    {
        return new AbilityPreviewData(affectedCells: new(), freeAimTarget: targetingData.worldPosition);
    
    }
}
