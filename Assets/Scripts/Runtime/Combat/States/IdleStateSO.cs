using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Idle")]
public class IdleStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _targetingStateTemplate;

    public override void OnEnter()
    {
        manager.turnController.ClearPreview();
    }
    public override void OnExit()
    {
        Debug.Log($"Sono uscito dal Idle state");
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        if (element is InteractableGridElement interactable)
        {
            manager.combatCtx.selectedAbility = interactable.defaultCharacterAbility;
            manager.selectionCtx.currentCaster = interactable;
            manager.TransitionToState(_targetingStateTemplate);
        }
    }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }
}