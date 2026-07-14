using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Net Throwing Ability", menuName = "Abilities/Equipment/Net Throwing Ability")]
public class NetThrowingAbility : OffensiveAbilityBase
{
    [Header("Net Throwing configs")]
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _cooldown = 2;
    [SerializeField] private SlowPassiveSO _slowPassiveSO;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _selectionCtx.CurrentTargets.Count == _maxTargets;
    }

    public override bool IsValidTarget(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (targetingData?.selectedTarget is not IHealthOwner ho || ho.Health == null || !ho.Health.IsAlive) return false;
        return targetingData.Value.selectedTarget is ITurnAgent;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return new NetThrowCommand(caster, _selectionCtx.CurrentTargets, _projectilePrefab, _cooldown, _slowPassiveSO, trajectoryConfigData);
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return new AbilityPreviewData(affectedCells: new(), interactionArea: new(), freeAimTargets: _selectionCtx.CurrentTargets);
    }
}
