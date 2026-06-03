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

        int overcapCritBonus = 0;
        if (caster is IAwakable awakable)
        {
            int extraPoints = Mathf.Max(0, awakable.CurrentAwakeningPoints - awakable.MaxAwakeningPoints);
            overcapCritBonus = (int)MathUtils.CalculateOvercapBonus(extraPoints);
        }

        Debug.Log($"[ShootCommand] Overcap crit bonus: +{overcapCritBonus}% (extraPoints: {(caster is IAwakable aw ? Mathf.Max(0, aw.CurrentAwakeningPoints - aw.MaxAwakeningPoints) : 0)})");

        return new ShootCommand(caster, _selectionCtx.CurrentTargets, _projectileConfig, _cooldown,
                                _baseDMG, _baseDMGType, offStats, trajectoryConfigData, overcapCritBonus);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    
    }
}
