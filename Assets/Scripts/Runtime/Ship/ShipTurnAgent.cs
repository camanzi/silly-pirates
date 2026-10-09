using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Makes the ship a turn agent: it joins the turn queue and gets a turn of its own.
///
/// For now the turn is empty and automatic: it starts, holds for a moment so the camera can settle on the ship,
/// and ends by itself. The player has no agency during it (see <see cref="TurnAgentRoles.HasPlayerAgency"/>), so
/// the HUD and the camera treat it like an enemy turn.
///
/// The ship is deliberately not an <see cref="IHealthOwner"/> nor an <see cref="ITargettable"/>: the enemy AI
/// skips it, and it does not count towards the combat outcome (TurnOrderQueries ignores agents with no health).
/// It lives next to <see cref="ShipController"/>, which stays the tilemap manager.
/// </summary>
public class ShipTurnAgent : MonoBehaviour, ITurnAgent
{
    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;
    [SerializeField] private string _displayName = "Ship";

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;
    [SerializeField] private AgentAVDeltaEventChannel _onAgilityChangedChannel;
    [SerializeField] private TurnStateSO _currentTurnState;

    [Header("Proximity Logic")]
    [Tooltip("Raised empty at the start of the ship's turn, so the equipment menus of the previous crew member close")]
    [SerializeField] private InteractableProximityEventChannel _proximityChannel;

    [Header("Turn behaviour")]
    [Tooltip("Seconds the ship holds its (empty) turn before ending it, so the camera can settle on it")]
    [SerializeField] [Min(0f)] private float _turnHoldSeconds = 0.75f;

    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;
    public TurnAgentDataSO AgentData => _agentData;
    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;
    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;

    // The ship has no action points to show: AP changes are never broadcast.
    public IntEventChannel OnAPChanged => null;
    public InteractableProximityEventChannel ProximityChannel => _proximityChannel;
    public string DisplayName => _displayName;

    // No HealthController: the ship cannot be hit or sunk, and is ignored by the outcome evaluation.
    public HealthController Health => null;

    public int RemainingActionPoints { get; set; }

    public int EffectiveAgility
    {
        get
        {
            if (_passiveAbilityController != null) _passiveAbilityController.GetModifiers(_agilityModifiers);
            else _agilityModifiers.Clear();

            return StatUtils.EvaluateAgility(_agentData != null ? _agentData.InitialAgility : 0, _agilityModifiers);
        }
    }

    // Optional: today the ship has no PassiveAbilityController and its agility is the base one. Before adding
    // one, check that the controller does not assume a GridCharacter owner.
    private PassiveAbilityController _passiveAbilityController;
    private readonly List<IAgilityModifier> _agilityModifiers = new();
    private int _lastKnownEffectiveAgility;

    private void Awake()
    {
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
    }

    private void OnEnable()
    {
        OnCombatJoin();
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged += HandlePassivesChanged;
    }

    private void OnDisable()
    {
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged -= HandlePassivesChanged;
    }

    private void Start() => _lastKnownEffectiveAgility = EffectiveAgility;

    private void HandlePassivesChanged()
    {
        int newAgility = EffectiveAgility;
        if (newAgility == _lastKnownEffectiveAgility) return;
        float avDelta = StatUtils.BaseAVDelta(_lastKnownEffectiveAgility, newAgility);
        _onAgilityChangedChannel?.RaiseEvent(new AgentAVDeltaPayload { Agent = this, AVDelta = avDelta });
        _lastKnownEffectiveAgility = newAgility;
    }

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave() => this.HandleCombatLeave();

    public void OnStartingTurn()
    {
        this.HandleStartingTurn();
        this.EmitProximityCheck(ProximityPayload.Empty);
        _ = RunTurnAsync(destroyCancellationToken);
    }

    public void OnContinuingTurn() => _ = RunTurnAsync(destroyCancellationToken);

    public void OnEndingTurn() { }

    /// <summary>
    /// The empty turn. Never ends synchronously: TurnController raises NotifyAgentActivated right after
    /// OnStartingTurn returns, and a turn already over by then would confuse every listener of that event.
    /// SignalTurnEnd sits in a finally, like in EnemyTurnDriver, so the turn always ends.
    /// </summary>
    private async Awaitable RunTurnAsync(CancellationToken token)
    {
        try
        {
            await Awaitable.WaitForSecondsAsync(_turnHoldSeconds, token);
        }
        catch (OperationCanceledException)
        {
            // Destroyed mid-turn (scene unloaded): the turn loop is being cancelled too.
            return;
        }
        finally
        {
            if (!token.IsCancellationRequested) _currentTurnState?.SignalTurnEnd();
        }
    }
}
