using System;
using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable, IPassableOccupant, ITargettable, ITurnAgent, IAbilityHolder, IHealthOwner, IAwakeningModifierHolder
{
    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;
    [SerializeField] private string _displayName;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;
    [SerializeField] private IntEventChannel _onAPChangedEventChannel;
    [SerializeField] private IntEventChannel _onMovementPointsChangedEventChannel;
    [SerializeField] private AgentAVDeltaEventChannel _onAgilityChangedChannel;

    public TurnAgentDataSO AgentData => _agentData;
    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;
    public string DisplayName => _displayName;

    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;

    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;

    public IntEventChannel OnAPChanged => _onAPChangedEventChannel;

    public IntEventChannel OnMovementPointsChanged => _onMovementPointsChangedEventChannel;

    public InteractableProximityEventChannel ProximityChannel => _proximityChannel;

    public AbilityController ActiveAbilityController => AbilityController;
    public PassiveAbilityController PassiveAbilityController => _passiveAbilityController;

    public int RemainingMovementPoints
    {
        get => _remainingMovementPoints;
        set
        {
            _remainingMovementPoints = value;
            OnMovementPointsChanged?.RaiseEvent(_remainingMovementPoints);
        }
    }

    public int RemainingActionPoints
    {
        get => _remainingActionPoints;
        set
        {
            _remainingActionPoints = value;
            OnAPChanged?.RaiseEvent(_remainingActionPoints);
        }
    }

    public int EffectiveAgility
    {
        get
        {
            int totalFlat = 0;
            float totalPct = 0f;
            if (_passiveAbilityController)
            {
                _passiveAbilityController.GetModifiers(_agilityModifiers);
                foreach (var m in _agilityModifiers)
                {
                    totalFlat += m.GetFlatAgilityBonus();
                    totalPct  += m.GetPercentageAgilityBonus();
                }
            }
            float modified = (AgentData.InitialAgility + totalFlat) * (1f + totalPct / 100f);
            return Mathf.Max(1, Mathf.RoundToInt(modified));
        }
    }
    public int EffectiveEvasion
    {
        get
        {
            int total = 0;
            if (_passiveAbilityController)
            {
                _passiveAbilityController.GetModifiers(_evasionModifiers);
                for (int i = 0; i < _evasionModifiers.Count; i++)
                    total += _evasionModifiers[i].GetEvasionBonus();
            }
            return AgentData.BaseEvasion + total;
        }
    }

    private int _remainingMovementPoints;
    private int _remainingActionPoints;
    private int _lastKnownEffectiveAgility;
    private DirectionalSpriteController _directionalSpriteController;
    private PassiveAbilityController _passiveAbilityController;
    private HealthController _healthController;
    private CharacterLifecycleAnimator _lifecycleAnimator;
    private Tween _moveTween;
    private readonly List<IMovementModifier> _movementModifiers = new();
    private readonly List<IAgilityModifier> _agilityModifiers = new();
    private readonly List<IEvasionModifier> _evasionModifiers = new();
    private readonly List<IOnCellEntered> _cellEnteredHandlers = new();
    private readonly List<IOnCellExited> _cellExitedHandlers = new();
    private readonly List<IOnTurnStart> _turnStartHandlers = new();
    private readonly List<IAwakeningModifier> _awakeningModifiers = new();

    public event Action OnAwakeningModifiersChanged;

    public int TotalAwakeningBonus
    {
        get
        {
            int total = 0;
            for (int i = 0; i < _awakeningModifiers.Count; i++)
                total += _awakeningModifiers[i].GetAwakeningBonus();
            return total;
        }
    }

    public void AddAwakeningModifier(IAwakeningModifier modifier)
    {
        _awakeningModifiers.Add(modifier);
        OnAwakeningModifiersChanged?.Invoke();
    }

    public void RemoveAwakeningModifier(IAwakeningModifier modifier)
    {
        _awakeningModifiers.Remove(modifier);
        OnAwakeningModifiersChanged?.Invoke();
    }

    public void NotifyAwakeningContribution(IAwakable awakable)
    {
        for (int i = 0; i < _awakeningModifiers.Count; i++)
            if (_awakeningModifiers[i] is IAwakeningContributionEffect effect)
                effect.OnContribution(awakable);
    }

    public HealthController Health => _healthController;

    protected override void Awake()
    {
        base.Awake();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _healthController = GetComponent<HealthController>();
        _lifecycleAnimator = GetComponent<CharacterLifecycleAnimator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OnCombatJoin();
        if (_healthController != null) _healthController.OnDeath += OnCombatLeave;
        if (_healthController != null) _healthController.OnRevive += OnCombatRevive;
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged += HandlePassivesChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_healthController != null) _healthController.OnDeath -= OnCombatLeave;
        if (_healthController != null) _healthController.OnRevive -= OnCombatRevive;
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged -= HandlePassivesChanged;
    }

    private void Start() => _lastKnownEffectiveAgility = EffectiveAgility;

    private void HandlePassivesChanged()
    {
        int newAgility = EffectiveAgility;
        if (newAgility == _lastKnownEffectiveAgility) return;
        float oldBaseAV = 10000f / Mathf.Max(1, _lastKnownEffectiveAgility);
        float newBaseAV = 10000f / Mathf.Max(1, newAgility);
        _onAgilityChangedChannel?.RaiseEvent(new AgentAVDeltaPayload { Agent = this, AVDelta = newBaseAV - oldBaseAV });
        _lastKnownEffectiveAgility = newAgility;
    }

    public Awaitable MoveTo(IEnumerable<Vector3> path, CancellationToken token)
        => MoveTo(path, null, token);

    public async Awaitable MoveTo(IEnumerable<Vector3> path, Func<Vector3Int, int> costGetter, CancellationToken token)
    {
        _moveTween.Stop();

        _directionalSpriteController.PlayAnimation(EAnimation.Walk);

        foreach (Vector3 node in path)
        {
            if (token.IsCancellationRequested) break;

            Vector3 targetPosition = _floorTilemap.GetCellCenterWorld(Vector3Int.FloorToInt(node));

            transform.LookAt(targetPosition);
            await Tween.Position(transform, targetPosition, duration: 0.5f, ease: Ease.Linear);

            gridPosition = Vector3Int.FloorToInt(node);

            RemainingMovementPoints -= costGetter?.Invoke(Vector3Int.FloorToInt(node)) ?? 1;
        }

        _directionalSpriteController.PlayAnimation(EAnimation.Idle);
    }

    protected override void OnGridPositionChanged(Vector3Int prevPosition, Vector3Int newPosition)
    {
        if (_agentData != null)
            this.EmitProximityCheck(new ProximityPayload(this, _agentData.InteractionRange));

        if (_passiveAbilityController)
        {
            _passiveAbilityController.GetModifiers(_cellExitedHandlers);
            foreach (var h in _cellExitedHandlers)
                h.OnCellExited(prevPosition);

            _passiveAbilityController.GetModifiers(_cellEnteredHandlers);
            foreach (var h in _cellEnteredHandlers)
                h.OnCellEntered(newPosition);
        }
    }

    public void OnCombatJoin()
    {
        _lifecycleAnimator?.Play(LifecyclePhase.Spawn);
        this.HandleCombatJoin();
    }

    public void OnCombatLeave()
    {
        _directionalSpriteController?.SetDeadVisual();
        _lifecycleAnimator?.Play(LifecyclePhase.Leave); // fire-and-forget: gli alleati non vengono disattivati
        this.HandleCombatLeave();
    }

    public void OnCombatRevive()
    {
        _lifecycleAnimator?.ResetToRest();
        _directionalSpriteController?.ResetVisual();
        _directionalSpriteController?.PlayAnimation(EAnimation.Idle);
        OnCombatJoin();
    }

    public void OnStartingTurn()
    {
        RemainingMovementPoints = AgentData.MaxMovementPoints + EvaluateMovementBonus();
        _healthController?.OnTurnStart();

        if (_passiveAbilityController)
        {
            _passiveAbilityController.GetModifiers(_turnStartHandlers);
            foreach (var h in _turnStartHandlers) h.OnTurnStart();
        }

        this.HandleStartingTurn();
        this.EmitProximityCheck(new ProximityPayload(this, AgentData.InteractionRange));
    }

    public void OnContinuingTurn() { }

    public void OnEndingTurn()
    {
        _passiveAbilityController?.HandleOwnerTurnEnd();
    }

    public int EvaluateMovementBonus()
    {
        int bonusMovement = 0;
        if (_passiveAbilityController)
        {
            _passiveAbilityController.GetModifiers(_movementModifiers);
            for (int i = 0; i < _movementModifiers.Count; i++)
                bonusMovement += _movementModifiers[i].GetMovementBonus();

#if UNITY_EDITOR
            Debug.Log($"Bonus Movement: {bonusMovement}");
#endif
        }

        return bonusMovement;
    }
}
