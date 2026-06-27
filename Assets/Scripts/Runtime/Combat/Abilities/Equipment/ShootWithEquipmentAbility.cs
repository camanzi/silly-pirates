using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    [Header("Cannon ability configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private DamageTypeProjectileConfigSO _projectileConfig;
    [SerializeField] private int _cooldown = 2;
    [SerializeField] private int _baseDMG;
    [SerializeField] private DamageType _baseDMGType;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _selectionCtx.CurrentTargets.Count == _maxTargets;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var offStats = (caster as IEquipmentStats)?.StatsConfig as IOffensiveEquipmentStats;
        int overcapAccuracyBonus = (caster as IHitRateOwner)?.EffectiveHitBonus ?? 0;

        return new ShootCommand(caster, _selectionCtx.CurrentTargets, _projectileConfig, _cooldown,
                                _baseDMG, _baseDMGType, offStats, trajectoryConfigData, overcapAccuracyBonus);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    }

    public override float? GetHitChance(IInteractableElement caster, TargetingData targetingData)
    {
        int baseHit = ((caster as IEquipmentStats)?.StatsConfig as IOffensiveEquipmentStats)?.BaseHitPercentage ?? 100;
        int hitBonus = (caster as IHitRateOwner)?.EffectiveHitBonus ?? 0;
        return Mathf.Min(100f, baseHit + hitBonus);
    }
}
