using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Targeting")]
public class TargetingStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _idleStateTemplate;
    [SerializeField] private CombatStateSO _executionStateTemplate;
    [SerializeField] private BoolEventChannel _targetingStateChannel;

    private AbilityBase _activeAbility;
    private IInteractableElement _activeCaster;
    private object _activeCache;

    public override void OnEnter()
    {
        base.OnEnter();
        _targetingStateChannel?.RaiseEvent(true);
        CombatContext ctx = manager.CombatCtx;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        TargetingData essentialTD = new TargetingData(caster);

        AbilityPreviewData previewData = ctx.SelectedAbility.GetPreviewData(caster, essentialTD, ref ctx.AbilityCache);
        _activeAbility = ctx.SelectedAbility;
        _activeCaster = caster;
        _activeCache = ctx.AbilityCache;
        manager.AabilityRenderer.DrawAbilityPreview(previewData, ctx.SelectedAbility, caster, essentialTD, false);
    }

    public override void OnExit()
    {
        _targetingStateChannel?.RaiseEvent(false);
        _activeAbility?.OnTargetingExit(_activeCaster, ref _activeCache);
        _activeAbility = null;
        _activeCaster = null;
        _activeCache = null;
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        SelectionContextSO selectionCtx = manager.SelectionCtx;

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
        CombatContext ctx = manager.CombatCtx;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        AbilityPreviewData previewData = ctx.SelectedAbility.GetPreviewData(caster, data, ref ctx.AbilityCache);
        bool canExecute = ctx.SelectedAbility.CanExecute(caster, data, ref ctx.AbilityCache);
        _activeAbility = ctx.SelectedAbility;
        _activeCaster = caster;
        _activeCache = ctx.AbilityCache;

        manager.AabilityRenderer.DrawAbilityPreview(previewData, ctx.SelectedAbility, caster, data, canExecute);
    }

    public override void HandleGlobalClick(TargetingData data) => OnClickBehavior(data);

    private void OnClickBehavior(TargetingData? data)
    {
        CombatContext combatCtx = manager.CombatCtx;
        AbilityBase selectedAbility = combatCtx.SelectedAbility;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        if (selectedAbility.CanExecute(caster, data, ref combatCtx.AbilityCache))
        {
            ICommand command = selectedAbility.CreateCommand(caster, data, ref combatCtx.AbilityCache);

            if (selectedAbility.IsPhaseCommand(command))
                return;

            manager.CommandQueue.AddCommand(command);
            manager.TransitionToState(_executionStateTemplate);
        }
    }

    public override void HandleSelectAbility(IInteractableElement element) { }
}