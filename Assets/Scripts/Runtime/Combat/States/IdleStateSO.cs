using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Idle")]
public class IdleStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _targetingStateTemplate;
    [SerializeField] private CombatStateSO _executionStateTemplate;

    public override void OnEnter()
    {
        base.OnEnter();
        manager.AabilityRenderer.ClearPreview();

        if (manager.CurrentTurnStateData.ActiveAgent is GridCharacter gc && gc.AgentData != null)
            gc.EmitProximityCheck(new ProximityPayload(gc, gc.AgentData.InteractionRange));
    }
    public override void OnExit() { }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        // FIXME Later, attenzione da capire cosa far fare quando seleziono un personaggio e non é il suo turno
        // Per il momento non faccio niente
        if (element is not ITurnAgent clickedAgent || clickedAgent != manager.CurrentTurnStateData.ActiveAgent)
            return;
        
        // Il prossimo step lo decide quindi la UI e non si selezionerá di base l'abilitá
        if (element is not InteractableGridElement interactable) return;
        
        EnterTargetingState(interactable);
    }

    public override void HandleSelectAbility(IInteractableElement element)
    {
        if (element is not InteractableGridElement interactable) return;

        EnterTargetingState(interactable);
    }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }

    public override void HandleRightClick() { }

    public override void HandleActiveAbilityRequest(ActiveAbilityRequestData data)
    {
        manager.CombatCtx.SelectedAbility = data.Ability;
        manager.SelectionCtx.CurrentCaster = data.Caster;

        if (!data.Ability.RequiresTargeting)
        {
            object cache = null;
            if (data.Ability.CanExecute(data.Caster, null, ref cache))
            {
                ICommand cmd = data.Ability.CreateCommand(data.Caster, null, ref cache);
                manager.CommandQueue.AddCommand(cmd);
            }
            manager.TransitionToState(_executionStateTemplate);
            return;
        }

        manager.TransitionToState(_targetingStateTemplate);
    }

    protected void EnterTargetingState(InteractableGridElement interactable)
    {
        manager.CombatCtx.SelectedAbility = interactable.DefaultCharacterAbility;
        manager.SelectionCtx.CurrentCaster = interactable;
        manager.TransitionToState(_targetingStateTemplate);
    }
}