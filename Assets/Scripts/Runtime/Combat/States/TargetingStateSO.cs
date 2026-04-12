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

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        SelectionContextSO selectionCtx = manager.SelectionCtx;

        if (selectionCtx.CurrentCaster == element)
        {
            manager.ClearCtxs(_idleStateTemplate);
            return;
        }

        if (element is ITargettable targettable)
        {
            selectionCtx.CurrentTargets.Add(targettable);
        }

        OnClickBehavior(null);
    }
    
    public override void HandleRightClick()
    {
        manager.ClearCtxs(_idleStateTemplate);
    }

    public override void HandlePointerMove(TargetingData data)
    {
        CombatContext combatCtx = manager.CombatCtx;
        SelectionContextSO selectionCtx = manager.SelectionCtx;

        manager.TurnController.DrawAbilityPreview(data, combatCtx.selectedAbility, selectionCtx.CurrentCaster);        
    }

    public override void HandleGlobalClick(TargetingData data) => OnClickBehavior(data);

    private void OnClickBehavior(TargetingData? data)
    {
        CombatContext combatCtx = manager.CombatCtx;
        AbilityBase selectedAbility = combatCtx.selectedAbility;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        if (selectedAbility.CanExecute(caster, data))
        {
            ICommand command = selectedAbility.CreateCommand(caster, data);
            
            manager.TurnController.AddCommand(command);
            manager.TransitionToState(_executionStateTemplate);
        }
    }
}