using UnityEngine;

public abstract class CombatStateSO : ScriptableObject
{
    [Header("Combat State configs")]
    [SerializeField] private bool _shouldShowUI;
    protected CombatStateManager manager;
    public bool ShouldShowUI => _shouldShowUI;
    public virtual void Init(CombatStateManager manager) => this.manager = manager;
    public virtual void OnEnter()
    {
        manager.ShowUIEventChannel.RaiseEvent(ShouldShowUI);
    }
    public abstract void OnExit();
    public abstract void OnUpdate();
    public abstract void HandleRightClick();
    public abstract void HandleElementClick(IInteractableElement element);
    public abstract void HandleSelectAbility(IInteractableElement element);
    public virtual void HandleActiveAbilityRequest(ActiveAbilityRequestData data) { }
    public abstract void HandlePointerMove(TargetingData data);
    public abstract void HandleGlobalClick(TargetingData data);

    protected void DrawAbilityPreview(AbilityBase ability, IInteractableElement caster, TargetingData data, ref object cache, bool computeCanExecute)
    {
        AbilityPreviewData previewData = ability.GetPreviewData(caster, data, ref cache);
        bool canExecute = computeCanExecute && ability.CanExecute(caster, data, ref cache);

        var costPayload = canExecute
            ? new AbilityCostPayload(ability.ActionPointCost, ability.GetMovementPointCost(previewData, ref cache))
            : AbilityCostPayload.Empty;
        manager.AbilityCostChannel?.RaiseEvent(costPayload);

        manager.AabilityRenderer.DrawAbilityPreview(previewData, ability, caster, data, canExecute);
    }

    protected void ClearAbilityPreview()
    {
        manager.AabilityRenderer.ClearPreview();
        manager.AbilityCostChannel?.RaiseEvent(AbilityCostPayload.Empty);
    }

    protected bool TryExecuteAbilityOnClick(AbilityBase ability, IInteractableElement caster, TargetingData? data, ref object cache, CombatStateSO executionStateTemplate)
    {
        if (!ability.CanExecute(caster, data, ref cache)) return false;

        ICommand command = ability.CreateCommand(caster, data, ref cache);
        if (ability.IsPhaseCommand(command)) return false;

        manager.CommandQueue.AddCommand(command);
        manager.TransitionToState(executionStateTemplate);
        return true;
    }
}