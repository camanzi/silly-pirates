using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Idle")]
public class IdleStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _targetingStateTemplate;
    [SerializeField] private TurnStateSO _currentTurnStateData;

    public override void OnEnter()
    {
        manager.TurnController.ClearPreview();
    }
    public override void OnExit()
    {
        Debug.Log($"Sono uscito dal Idle state");
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        // FIXME Later, attenzione da capire cosa far fare quando seleziono un personaggio e non é il suo turno
        // Per il momento non lo seleziono e ignoro la selezione
        if (element is not ITurnAgent clickedAgent || clickedAgent != manager.CurrentTurnStateData.ActiveAgent)
            return;
        
        // Il prossimo step lo decide quindi la UI e non si selezionerá di base l'abilitá
        if (element is InteractableGridElement interactable)
        {
            manager.CombatCtx.selectedAbility = interactable.defaultCharacterAbility;
            manager.SelectionCtx.CurrentCaster = interactable;
            manager.TransitionToState(_targetingStateTemplate);
        }
    }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }

    public override void HandleRightClick() { }
}