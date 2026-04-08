using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Combat/States/Targeting")]
public class TargetingStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _idleStateTemplate;
    [SerializeField] private CombatStateSO _executionStateTemplate;

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
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) manager.ClearCtxs(_idleStateTemplate);
    }

    public override void HandleElementClick(IInteractableElement element)
    {
        SelectionContextSO selectionCtx = manager.selectionCtx;

        if (selectionCtx.CurrentCaster == element) manager.ClearCtxs(_idleStateTemplate);

        if (element is ITargettable targettable)
        {
            selectionCtx.CurrentTargets.Add(targettable);
        }

        OnClickBehavior(null);
    }

    public override void HandlePointerMove(TargetingData data)
    {
        CombatContext combatCtx = manager.combatCtx;
        SelectionContextSO selectionCtx = manager.selectionCtx;

        manager.turnController.DrawAbilityPreview(data, combatCtx.selectedAbility, selectionCtx.CurrentCaster);        
    }

    public override void HandleGlobalClick(TargetingData data) => OnClickBehavior(data);

    private void OnClickBehavior(TargetingData? data)
    {
        CombatContext combatCtx = manager.combatCtx;
        AbilityBase selectedAbility = combatCtx.selectedAbility;
        IInteractableElement caster = manager.selectionCtx.CurrentCaster;

        if (selectedAbility.CanExecute(caster, data))
        {
            ICommand command = selectedAbility.CreateCommand(caster, data);
            
            manager.turnController.AddCommand(command);
            manager.TransitionToState(_executionStateTemplate);
        }
    }
}