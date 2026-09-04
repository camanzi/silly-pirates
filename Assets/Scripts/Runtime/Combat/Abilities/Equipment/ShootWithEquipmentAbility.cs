using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : OffensiveAbilityBase, IMultiTargetAbility, IOffensiveAbility
{
    [Header("Cannon ability configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private DamageTypeProjectileConfigSO _projectileConfig;
    [SerializeField] private int _cooldown = 2;
    [SerializeField] private int _baseDMG;
    [SerializeField] private DamageType _baseDMGType;

    [Tooltip("Suono dello sparo, riprodotto sul frame esatto del colpo (non all'avvio dell'abilita')")]
    [SerializeField] private SoundEventSO _fireSfx;

    [Tooltip("Nube di fumo emessa dalla bocca del cannone sul frame dello sparo.")]
    [SerializeField] private VFXController _muzzleVfx;

    public int MaxTargets => _maxTargets;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _selectionCtx.CurrentTargets.Count == MaxTargets;
    }

    public override bool IsValidTarget(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return targetingData?.selectedTarget is IHealthOwner ho && ho.Health != null && ho.Health.IsAlive;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        int effectiveAccuracy = (caster as IAccuracyOwner)?.EffectiveAccuracy ?? 50;

        return new ShootCommand(caster, _selectionCtx.CurrentTargets, _projectileConfig, _cooldown,
                                _baseDMG, _baseDMGType, trajectoryConfigData, effectiveAccuracy,
                                _fireSfx, _sfxChannel, _muzzleVfx, _vfxChannel);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    }

    // Stessa regola di ShootCommand.ResolveDMGType: l'elemento vero e' quello del cannone montato.
    public DamageType ResolveDamageElement(IInteractableElement caster)
        => DamageTypeResolver.Resolve(caster, _baseDMGType);
}
