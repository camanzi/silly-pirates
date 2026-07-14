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

        DrawAbilityPreview(ctx.SelectedAbility, caster, essentialTD, ref ctx.AbilityCache, computeCanExecute: false);
        _activeAbility = ctx.SelectedAbility;
        _activeCaster = caster;
        _activeCache = ctx.AbilityCache;
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
        CombatContext ctx = manager.CombatCtx;

        if (element is ITargettable targettable)
        {
            var td = new TargetingData { selectedTarget = targettable };
            if (!ctx.SelectedAbility.IsValidTarget(selectionCtx.CurrentCaster, td, ref ctx.AbilityCache)) return;
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
        if (!data.worldPosition.HasValue)
        {
            ClearAbilityPreview();
            return;
        }

        CombatContext ctx = manager.CombatCtx;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        DrawAbilityPreview(ctx.SelectedAbility, caster, data, ref ctx.AbilityCache, computeCanExecute: true);
        _activeAbility = ctx.SelectedAbility;
        _activeCaster = caster;
        _activeCache = ctx.AbilityCache;
    }

    public override void HandleGlobalClick(TargetingData data) => OnClickBehavior(data);

    private void OnClickBehavior(TargetingData? data)
    {
        CombatContext combatCtx = manager.CombatCtx;
        AbilityBase selectedAbility = combatCtx.SelectedAbility;
        IInteractableElement caster = manager.SelectionCtx.CurrentCaster;

        TryExecuteAbilityOnClick(selectedAbility, caster, data, ref combatCtx.AbilityCache, _executionStateTemplate);
    }

    public override void HandleSelectAbility(IInteractableElement element) { }
}