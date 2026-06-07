using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class TelegraphedEnemyAbility : EnemyAbilityBase
{
    [Header("Telegraph Visual")]
    [SerializeField] protected CellEffectEventChannel _cellEffectChannel;
    [SerializeField] protected Material _threatMaterial;

    [Header("Step Flavor Text")]
    [SerializeField][TextArea] private string _stepTwoFlavorText;

    [Header("AI Context")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;

    private readonly Dictionary<HostileCharacter, TelegraphState> _pendingStates = new();
    private readonly Dictionary<HostileCharacter, Action<EnemyPartSO>> _partBrokenHandlers = new();
    private readonly Dictionary<HostileCharacter, Action> _deathHandlers = new();
    private HostileCharacter _lastScoredCaster;

    public class TelegraphState
    {
        public List<Vector3Int> AllCells;
    }

    protected virtual void OnEnable()
    {
        foreach (var (hostile, handler) in _partBrokenHandlers)
            if (hostile is IPartOwner partOwner) partOwner.OnPartBroken -= handler;
        _partBrokenHandlers.Clear();
        foreach (var (hostile, handler) in _deathHandlers)
            if (hostile.Health != null) hostile.Health.OnDeath -= handler;
        _deathHandlers.Clear();
        _pendingStates.Clear();
        _lastScoredCaster = null;
    }

    public override string FlavorText =>
        _lastScoredCaster != null && _pendingStates.ContainsKey(_lastScoredCaster)
            ? _stepTwoFlavorText
            : base.FlavorText;

    protected abstract string ThreatKey { get; }

    protected abstract float ComputeTelegraph(AIContext context, out TargetingData targeting, out TelegraphState state);

    protected abstract ICommand CreateStrikeCommand(HostileCharacter caster, TelegraphState state);

    protected virtual ICommand CreateTelegraphCommand(HostileCharacter caster, TelegraphState state) =>
        new TelegraphCommand(state.AllCells, _cellEffectChannel, _threatMaterial, ThreatKey);

    protected override bool MeetsPreconditions(AIContext context)
    {
        bool meets = base.MeetsPreconditions(context);
        if (!meets && _pendingStates.ContainsKey(context.Caster))
            RollbackPending(context.Caster);
        return meets;
    }

    private void SubscribePartBreak(HostileCharacter hostile)
    {
        if (hostile is IPartOwner partOwner)
        {
            void partHandler(EnemyPartSO part) { if (IsRequiredPart(part)) RollbackPending(hostile); }
            _partBrokenHandlers[hostile] = partHandler;
            partOwner.OnPartBroken += partHandler;
        }

        void deathHandler() { RollbackPending(hostile); }
        _deathHandlers[hostile] = deathHandler;
        hostile.Health.OnDeath += deathHandler;
    }

    private void UnsubscribePartBreak(HostileCharacter hostile)
    {
        if (_partBrokenHandlers.TryGetValue(hostile, out var partHandler))
        {
            if (hostile is IPartOwner partOwner) partOwner.OnPartBroken -= partHandler;
            _partBrokenHandlers.Remove(hostile);
        }

        if (_deathHandlers.TryGetValue(hostile, out var deathHandler))
        {
            if (hostile.Health != null) hostile.Health.OnDeath -= deathHandler;
            _deathHandlers.Remove(hostile);
        }
    }

    private void RollbackPending(HostileCharacter hostile)
    {
        UnsubscribePartBreak(hostile);
        if (!_pendingStates.TryGetValue(hostile, out var state)) return;
        _pendingStates.Remove(hostile);
        _cellEffectChannel?.RaiseEvent(new CellEffectPayload { Key = ThreatKey, Cells = null });
        OnPendingStateRolledBack(hostile, state);
    }

    protected virtual void OnPendingStateRolledBack(HostileCharacter caster, TelegraphState state) { }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        _lastScoredCaster = context.Caster;

        if (_pendingStates.ContainsKey(context.Caster))
        {
            targeting = TargetingData.Empty;
            return 100f;
        }

        return ComputeTelegraph(context, out targeting, out _);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
        => true;

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;

        if (_pendingStates.TryGetValue(hostile, out var existing))
        {
            _pendingStates.Remove(hostile);
            UnsubscribePartBreak(hostile);
            return CreateStrikeCommand(hostile, existing);
        }

        var context = new AIContext(hostile, _turnOrderData, _gridStateData);
        float score = ComputeTelegraph(context, out _, out var newState);

        if (score == float.NegativeInfinity || newState == null)
            return null;

        _pendingStates[hostile] = newState;
        SubscribePartBreak(hostile);
        return CreateTelegraphCommand(hostile, newState);
    }
}
