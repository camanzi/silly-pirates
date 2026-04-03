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

    public override void OnUpdate()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearCtxsAndReturnIdle();
        }
    }

    public override void HandleElementClick(IInteractableElement element)
    {
        SelectionContextSO selectionCtx = manager.selectionCtx;

       if (selectionCtx.CurrentCaster == element) ClearCtxsAndReturnIdle();

       if (element is ITargettable targettable)
        {
            selectionCtx.CurrentTargets.Add(targettable);
        }
    }

    public override void HandlePointerMove(TargetingData data)
    {
        CombatContext combatCtx = manager.combatCtx;
        SelectionContextSO selectionCtx = manager.selectionCtx;

        manager.turnController.DrawAbilityPreview(data, combatCtx.selectedAbility, selectionCtx.CurrentCaster);        
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
        IInteractableElement caster = manager.selectionCtx.CurrentCaster;

        Vector3 target = selectedAbility.isFreeAim ? data.worldPosition : data.cellPosition;

        if (selectedAbility.CanExecute(caster, target))
        {
            ICommand command = selectedAbility.CreateCommand(caster, target, new List<IInteractableElement>());
            
            manager.turnController.AddCommand(command);
            _ = manager.turnController.ProcessQueueAsync();

            ClearCtxsAndReturnIdle();
        }
    }
}