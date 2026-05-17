using UnityEngine;

[CreateAssetMenu(fileName = "Touch Of The Stars Ability", menuName = "Abilities/Character/Actives/Touch Of The Stars Ability")]
public class TouchOfTheStarsAbility : AbilityBase
{
    [Header("Touch Of The Stars Configs")]
    [SerializeField] private int _apCost = 1;
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;

    public override int ActionPointCost => _apCost;
    public override bool RequiresTargeting => false;

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        return AbilityPreviewData.Empty;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return caster is ITurnAgent agent && agent.RemainingActionPoints >= _apCost;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints -= _apCost;

        return new TouchOfTheStarsCommand(caster, _apCost, _onAnyTurnEnded);
    }
}
