using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MultiStep Ability", menuName = "Abilities/Enemy/Multi Step Ability")]
public class MultiStepAbility : EnemyAbilityBase, IThreatenedAreaProvider
{
    [SerializeField] private List<MultiStepAbilityStepSO> _steps;

    [Header("AI Context")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;

    private readonly Dictionary<HostileCharacter, int> _stepIndex = new();
    private readonly Dictionary<HostileCharacter, StepState> _state = new();
    private readonly Dictionary<HostileCharacter, StepState> _lastResolvedStates = new();
    private readonly Dictionary<HostileCharacter, Action<EnemyPartSO>> _partBrokenHandlers = new();
    private readonly Dictionary<HostileCharacter, Action> _deathHandlers = new();
    private MultiStepAbilityStepSO _lastExecutedStep;

    public bool TryGetThreatenedWorldPoints(HostileCharacter caster, out IReadOnlyList<Vector3> points)
    {
        if (_lastResolvedStates.TryGetValue(caster, out var state) && state.AffectedWorldPoints is { Count: > 0 })
        { points = state.AffectedWorldPoints; return true; }
        points = null;
        return false;
    }

    protected virtual void OnEnable()
    {
        foreach (var (hostile, handler) in _partBrokenHandlers)
            if (hostile is IPartOwner po) po.OnPartBroken -= handler;
        _partBrokenHandlers.Clear();
        foreach (var (hostile, handler) in _deathHandlers)
            if (hostile.Health != null) hostile.Health.OnDeath -= handler;
        _deathHandlers.Clear();
        _stepIndex.Clear();
        _state.Clear();
        _lastResolvedStates.Clear();
        _lastExecutedStep = null;
    }

    public override string FlavorText =>
        _lastExecutedStep != null && !string.IsNullOrEmpty(_lastExecutedStep.FlavorTextOverride)
            ? _lastExecutedStep.FlavorTextOverride : base.FlavorText;

    public override CameraCueType CameraCue =>
        _lastExecutedStep != null && _lastExecutedStep.CameraCueTypeOverride.HasValue
            ? _lastExecutedStep.CameraCueTypeOverride.Value : base.CameraCue;

    public override CameraCueProfileSO CameraCueProfile =>
        _lastExecutedStep != null && _lastExecutedStep.CameraCueProfileOverride != null
            ? _lastExecutedStep.CameraCueProfileOverride : base.CameraCueProfile;

    protected override bool MeetsPreconditions(AIContext context)
    {
        bool meets = base.MeetsPreconditions(context);
        if (!meets && _stepIndex.ContainsKey(context.Caster)) RollbackPending(context.Caster);
        return meets;
    }

    private void SubscribePartBreak(HostileCharacter hostile)
    {
        if (hostile is IPartOwner po)
        {
            void partHandler(EnemyPartSO part) { if (IsRequiredPart(part)) RollbackPending(hostile); }
            _partBrokenHandlers[hostile] = partHandler;
            po.OnPartBroken += partHandler;
        }
        void deathHandler() { RollbackPending(hostile); }
        _deathHandlers[hostile] = deathHandler;
        hostile.Health.OnDeath += deathHandler;
    }

    private void UnsubscribePartBreak(HostileCharacter hostile)
    {
        if (_partBrokenHandlers.TryGetValue(hostile, out var ph))
        { if (hostile is IPartOwner po) po.OnPartBroken -= ph; _partBrokenHandlers.Remove(hostile); }
        if (_deathHandlers.TryGetValue(hostile, out var dh))
        { if (hostile.Health != null) hostile.Health.OnDeath -= dh; _deathHandlers.Remove(hostile); }
    }

    private void RollbackPending(HostileCharacter hostile)
    {
        UnsubscribePartBreak(hostile);
        _lastResolvedStates.Remove(hostile);
        if (!_stepIndex.TryGetValue(hostile, out int idx) || idx <= 0) return;
        if (!_state.TryGetValue(hostile, out var state)) return;
        _stepIndex.Remove(hostile);
        _state.Remove(hostile);
        _steps[idx - 1].OnRolledBack(hostile, state);
    }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        if (_stepIndex.TryGetValue(context.Caster, out int idx) && idx > 0)
        { targeting = TargetingData.Empty; return 100f; }
        return _steps[0].ComputeScore(context, null, out targeting);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache) => true;

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;
        int idx = _stepIndex.TryGetValue(hostile, out var i) ? i : 0;

        if (idx > 0)
        {
            var state = _state[hostile];
            var step = _steps[idx];
            int next = idx + 1;
            if (next >= _steps.Count)
            { _stepIndex.Remove(hostile); _state.Remove(hostile); UnsubscribePartBreak(hostile); }
            else
            { _stepIndex[hostile] = next; }

            _lastResolvedStates[hostile] = state;
            _lastExecutedStep = step;
            return step.CreateCommand(hostile, state);
        }
        else
        {
            var context = new AIContext(hostile, _turnOrderData, _gridStateData);
            var state = new StepState();
            float score = _steps[0].ComputeScore(context, state, out _);
            if (score == float.NegativeInfinity) return null;

            state.Extra[MultiStepAbilityStepSO.RequiredPartTransformKey] = GetRequiredPartTransform(hostile);
            state.Extra[MultiStepAbilityStepSO.OwningAbilityKey] = this;

            ICommand cmd = _steps[0].CreateCommand(hostile, state);
            _lastExecutedStep = _steps[0];
            _lastResolvedStates[hostile] = state;

            if (_steps.Count > 1)
            { _stepIndex[hostile] = 1; _state[hostile] = state; SubscribePartBreak(hostile); }

            return cmd;
        }
    }
}
