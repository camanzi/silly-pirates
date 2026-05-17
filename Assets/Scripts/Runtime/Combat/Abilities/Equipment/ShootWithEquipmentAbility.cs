using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    [Header("Cannon ability configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private GameObject _projectile;
    [SerializeField] private int _cooldown = 2;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _selectionCtx.CurrentTargets.Count == _maxTargets;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var offStats = (caster as IEquipmentStats)?.StatsConfig as IOffensiveEquipmentStats;
        return new ShootCommand(caster, _selectionCtx.CurrentTargets, _projectile, _cooldown, offStats, trajectoryConfigData);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    
    }
}
