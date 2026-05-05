using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Idle")]
public class IdleStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _targetingStateTemplate;

    public override void OnEnter()
    {
        base.OnEnter();
        manager.TurnController.ClearPreview();
        Debug.Log($"Sono entrato da Idle state");
    }
    public override void OnExit()
    {
        Debug.Log($"Sono uscito dal Idle state");
    }

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

    // handle selected ability by Interaction Menu
    public override void HandleSelectAbility(IInteractableElement element)
    {
        if (element is not InteractableGridElement interactable) return;

        EnterTargetingState(interactable);
    }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }

    public override void HandleRightClick() { }

    protected void EnterTargetingState(InteractableGridElement interactable)
    {
        manager.CombatCtx.SelectedAbility = interactable.DefaultCharacterAbility;
        manager.SelectionCtx.CurrentCaster = interactable;
        manager.TransitionToState(_targetingStateTemplate);
    }
}