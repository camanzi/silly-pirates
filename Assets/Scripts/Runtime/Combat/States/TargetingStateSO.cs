using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Combat/States/Targeting")]
public class TargetingStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _idleStateTemplate;
    
    public override void OnEnter()
    {
        Debug.Log($"Sono entrato in Targeting state");
    }

    public override void OnExit()
    {
        Debug.Log($"Sono uscito da Targeting state");
    }

    // Se clicco col mouse destro allora Clear and back
    public override void OnUpdate()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearCtxsAndReturnIdle();
        }
    }

    public override void HandleElementClick(GridElement element)
    {
        SelectionContextSO selectionCtx = manager.selectionCtx;

       if (selectionCtx.currentCaster == element) ClearCtxsAndReturnIdle();
    }

    public override void HandlePointerMove(TargetingData data)
    {
        CombatContext combatCtx = manager.combatCtx;
        SelectionContextSO selectionCtx = manager.selectionCtx;

        manager.turnController.DrawAbilityPreview(data, combatCtx.selectedAbility, selectionCtx.currentCaster);        
    }

    private void ClearCtxsAndReturnIdle()
    {
        manager.selectionCtx.ClearCtx();
        manager.combatCtx.ClearCtx();
        manager.TransitionToState(_idleStateTemplate);
    }

    public override void HandleGlobalClick(TargetingData data)
    {
        CombatContext combatCtx = manager.combatCtx;
        AbilityBase selectedAbility = combatCtx.selectedAbility;
        InteractableGridElement caster = manager.selectionCtx.currentCaster;

        Vector3 target = selectedAbility.isFreeAim ? data.worldPosition : data.cellPosition;

        if (selectedAbility.CanExecute(caster, target))
        {
            ICommand command = selectedAbility.CreateCommand(caster, target, new List<GridElement>());
            
            manager.turnController.AddCommand(command);
            _ = manager.turnController.ProcessQueueAsync();

            ClearCtxsAndReturnIdle();
        }
    }
}