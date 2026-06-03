using UnityEngine;

[CreateAssetMenu(fileName = "Touch Of The Stars Ability", menuName = "Abilities/Character/Actives/Touch Of The Stars Ability")]
public class TouchOfTheStarsAbility : AbilityBase
{
    [Header("Touch Of The Stars Configs")]
    [SerializeField] private int _apCost = 1;
    [SerializeField] private TouchOfTheStarsPassiveSO _passiveSO;
    [SerializeField] private StellarDMGPassiveSO _stellarPassiveSO;
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
        if (caster is not GridCharacter character) return null;
        if (caster is not ITurnAgent turnAgent) return null;
        turnAgent.RemainingActionPoints -= _apCost;

        return new TouchOfTheStarsCommand(character.PassiveAbilityController, turnAgent, _passiveSO, _stellarPassiveSO, _onAnyTurnEnded, _apCost);
    }
}
