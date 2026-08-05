using System.Collections.Generic;
using UnityEngine;

public abstract class CombatStateSO : ScriptableObject
{
    [Header("Combat State configs")]
    [SerializeField] private bool _shouldShowUI;
    protected CombatStateManager manager;
    public bool ShouldShowUI => _shouldShowUI;
    protected virtual bool UsesTargetValidityOutline => true;
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
        bool isValidTarget = computeCanExecute && ability.IsValidTarget(caster, data, ref cache);
        bool canExecute = computeCanExecute && ability.CanExecute(caster, data, ref cache) && isValidTarget;

        if (computeCanExecute && data.selectedTarget is IInteractableElement targetElement)
        {
            OutlineState outlineState = UsesTargetValidityOutline
                ? (isValidTarget ? OutlineState.ValidTarget : OutlineState.InvalidTarget)
                : OutlineState.Default;
            targetElement.OutlinerHelper?.SetOutline(outlineState);
        }

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
        if (data.HasValue && !ability.IsValidTarget(caster, data, ref cache)) return false;

        ICommand command = ability.CreateCommand(caster, data, ref cache);
        if (ability.IsPhaseCommand(command)) return false;

        manager.CommandQueue.AddCommand(command);
        manager.CombatCtx.PendingCue = BuildExecutionCue(ability, caster, data, ref cache);
        manager.TransitionToState(executionStateTemplate);
        return true;
    }

    private AbilityExecutionCue BuildExecutionCue(AbilityBase ability, IInteractableElement caster, TargetingData? data, ref object cache)
    {
        AbilityPreviewData previewData = ability.GetPreviewData(caster, data ?? new TargetingData(caster), ref cache);

        List<ITargettable> targets = new();
        if (data?.selectedTarget != null) targets.Add(data.Value.selectedTarget);
        foreach (ITargettable target in manager.SelectionCtx.CurrentTargets)
        {
            if (!targets.Contains(target)) targets.Add(target);
        }

        return new AbilityExecutionCue(ability, caster, targets, ToWorldPoints(previewData.AffectedCells, caster), data?.worldPosition);
    }

    /// <summary>
    /// AffectedCells in the preview are grid cell coordinates, but AbilityExecutionCue.AffectedCells is
    /// consumed by the CameraDirector as world points (same contract as
    /// IThreatenedAreaProvider.TryGetThreatenedWorldPoints on the enemy side). Returning null when the
    /// conversion isn't possible is deliberate: the cue then falls back to TargetPoint, already a world point.
    /// </summary>
    private static List<Vector3> ToWorldPoints(List<Vector3> cells, IInteractableElement caster)
    {
        if (cells == null || cells.Count == 0) return null;
        if (caster is not GridElement gridElement || gridElement.activeTilemap == null) return null;

        var points = new List<Vector3>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            points.Add(gridElement.activeTilemap.GetCellCenterWorld(Vector3Int.FloorToInt(cells[i])));
        return points;
    }
}