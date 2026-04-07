using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    [Header("Cannon ability configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private GameObject _projectile;
    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData)
    {
        return _selectionCtx.CurrentTargets.Count == _maxTargets;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData)
    {
        return new ShootCommand(caster, _selectionCtx.CurrentTargets, _projectile, trajectoryConfigData);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData)
    {
        return new AbilityPreviewData(affectedCells: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    
    }
}
