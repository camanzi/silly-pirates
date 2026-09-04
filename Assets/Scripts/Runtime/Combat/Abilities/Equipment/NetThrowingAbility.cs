using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Net Throwing Ability", menuName = "Abilities/Equipment/Net Throwing Ability")]
public class NetThrowingAbility : OffensiveAbilityBase, IMultiTargetAbility, IOffensiveAbility
{
    [Header("Net Throwing configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _cooldown = 2;
    [SerializeField] private SlowPassiveSO _slowPassiveSO;

    [Tooltip("Nube di fumo emessa dalla bocca del cannone sul frame dello sparo.")]
    [SerializeField] private VFXController _muzzleVfx;

    public int MaxTargets => _maxTargets;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _selectionCtx.CurrentTargets.Count == MaxTargets;
    }

    public override bool IsValidTarget(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (targetingData?.selectedTarget is not IHealthOwner ho || ho.Health == null || !ho.Health.IsAlive) return false;
        return targetingData.Value.selectedTarget is ITurnAgent;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return new NetThrowCommand(caster, _selectionCtx.CurrentTargets, _projectilePrefab, _cooldown, _slowPassiveSO, trajectoryConfigData, _muzzleVfx, _vfxChannel);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    }

    // Ostile ma senza danno: applica solo uno slow, quindi non ha un elemento da annunciare.
    public DamageType ResolveDamageElement(IInteractableElement caster) => DamageType.None;
}
