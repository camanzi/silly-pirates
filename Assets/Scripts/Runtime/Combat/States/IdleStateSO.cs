using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Idle")]
public class IdleStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _targetingStateTemplate;
    [SerializeField] private CombatStateSO _executionStateTemplate;

    private AbilityBase _armedAbility;
    private InteractableGridElement _armedCaster;
    private object _armedCache;

    public override void Init(CombatStateManager manager)
    {
        base.Init(manager);
        manager.CurrentTurnStateData.OnAgentActivated.OnEventRaised += HandleAgentActivated;
    }

    private void OnDestroy()
    {
        if (manager != null && manager.CurrentTurnStateData != null)
            manager.CurrentTurnStateData.OnAgentActivated.OnEventRaised -= HandleAgentActivated;
    }

    private void HandleAgentActivated(ITurnAgent agent)
    {
        _armedAbility?.OnTargetingExit(_armedCaster, ref _armedCache);

        _armedCaster = agent as InteractableGridElement;
        _armedAbility = _armedCaster != null ? _armedCaster.DefaultCharacterAbility : null;
        _armedCache = null;

        manager.SelectionCtx.CurrentCaster = _armedAbility != null ? _armedCaster : null;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        manager.AabilityRenderer.ClearPreview();

        if (manager.CurrentTurnStateData.ActiveAgent is GridCharacter gc && gc.AgentData != null)
            gc.EmitProximityCheck(new ProximityPayload(gc, gc.AgentData.InteractionRange));

        if (_armedAbility != null)
            DrawAbilityPreview(_armedAbility, _armedCaster, new TargetingData(_armedCaster), ref _armedCache, computeCanExecute: false);
    }

    public override void OnExit()
    {
        _armedAbility?.OnTargetingExit(_armedCaster, ref _armedCache);
        _armedCache = null;
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element)
    {
        if (_armedAbility == null) return;

        if (element is ITargettable targettable) manager.SelectionCtx.CurrentTargets.Add(targettable);

        manager.CombatCtx.SelectedAbility = _armedAbility;
        TryExecuteAbilityOnClick(_armedAbility, _armedCaster, null, ref _armedCache, _executionStateTemplate);
    }

    public override void HandleSelectAbility(IInteractableElement element)
    {
        if (element is not InteractableGridElement interactable) return;

        EnterTargetingState(interactable);
    }

    public override void HandlePointerMove(TargetingData data)
    {
        if (_armedAbility == null) { manager.AabilityRenderer.ClearPreview(); return; }

        manager.CombatCtx.SelectedAbility = _armedAbility;
        DrawAbilityPreview(_armedAbility, _armedCaster, data, ref _armedCache, computeCanExecute: true);
    }

    public override void HandleGlobalClick(TargetingData data)
    {
        if (_armedAbility == null) return;

        manager.CombatCtx.SelectedAbility = _armedAbility;
        TryExecuteAbilityOnClick(_armedAbility, _armedCaster, data, ref _armedCache, _executionStateTemplate);
    }

    public override void HandleRightClick() { }

    public override void HandleActiveAbilityRequest(ActiveAbilityRequestData data)
    {
        manager.CombatCtx.SelectedAbility = data.Ability;
        manager.SelectionCtx.CurrentCaster = data.Caster;

        if (!data.Ability.RequiresTargeting)
        {
            object cache = null;
            if (data.Ability.CanExecute(data.Caster, null, ref cache))
            {
                ICommand cmd = data.Ability.CreateCommand(data.Caster, null, ref cache);
                manager.CommandQueue.AddCommand(cmd);
            }
            manager.TransitionToState(_executionStateTemplate);
            return;
        }

        manager.TransitionToState(_targetingStateTemplate);
    }

    protected void EnterTargetingState(InteractableGridElement interactable)
    {
        manager.CombatCtx.SelectedAbility = interactable.DefaultCharacterAbility;
        manager.SelectionCtx.CurrentCaster = interactable;
        manager.TransitionToState(_targetingStateTemplate);
    }
}