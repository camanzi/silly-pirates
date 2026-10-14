using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Makes the ship a turn agent: it joins the turn queue and gets a turn of its own.
///
/// The turn is automatic: it starts, holds for a moment so the camera can settle on the ship, fires the
/// Broadside (every awakened offensive equipment, see <see cref="ShipBroadsideController"/>) and ends by
/// itself. With nothing armed the broadside is a no-op and the turn is just the hold. The player has no
/// agency during it (see <see cref="TurnAgentRoles.HasPlayerAgency"/>), so the HUD and the camera treat it
/// like an enemy turn.
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

    [Header("Equipment")]
    [Tooltip("The awake equipment tied to the ship's turn: its agility modifiers count towards the ship's agility, and it is told when the ship has acted")]
    [SerializeField] private ShipTurnRosterSO _shipTurnRoster;

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
            // Appended after the passives: e.g. every awake AgilityBoosterEquipment.
            if (_shipTurnRoster != null) _shipTurnRoster.CollectModifiers(_agilityModifiers);

            return StatUtils.EvaluateAgility(_agentData != null ? _agentData.InitialAgility : 0, _agilityModifiers);
        }
    }

    // Optional: today the ship has no PassiveAbilityController, its agility modifiers come from the awake
    // equipment in _shipTurnRoster. Before adding one, check that the controller does not assume a
    // GridCharacter owner.
    private PassiveAbilityController _passiveAbilityController;
    private readonly List<IAgilityModifier> _agilityModifiers = new();
    private int _lastKnownEffectiveAgility;

    // Optional: without it the ship's turn stays empty.
    private ShipBroadsideController _broadside;

    private void Awake()
    {
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _broadside = GetComponent<ShipBroadsideController>();
    }

    private void OnEnable()
    {
        OnCombatJoin();
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged += HandleAgilityModifiersChanged;
        if (_shipTurnRoster != null) _shipTurnRoster.OnRosterChanged += HandleAgilityModifiersChanged;
    }

    private void OnDisable()
    {
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged -= HandleAgilityModifiersChanged;
        if (_shipTurnRoster != null) _shipTurnRoster.OnRosterChanged -= HandleAgilityModifiersChanged;
    }

    private void Start() => _lastKnownEffectiveAgility = EffectiveAgility;

    private void HandleAgilityModifiersChanged()
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
        _ = RunTurnAsync(destroyCancellationToken, runBroadside: true);
    }

    // Further actions of the same turn never fire again: the broadside belongs to the start of the turn.
    public void OnContinuingTurn() => _ = RunTurnAsync(destroyCancellationToken, runBroadside: false);

    /// <summary>
    /// Tells the equipment tied to the ship's turn that the ship has acted. This relies on the order of
    /// TurnController: OnEndingTurn runs before CompleteActiveTurn. An agility booster going on cooldown here
    /// raises an AV delta that AdjustAgentAV ignores (the active agent's AV is 0), and CompleteActiveTurn then
    /// computes the ship's next AV without the bonus: it only sped up the turn that just ended.
    /// </summary>
    public void OnEndingTurn()
    {
        if (_shipTurnRoster != null) _shipTurnRoster.NotifyShipTurnEnded();
    }

    /// <summary>
    /// Never ends synchronously: TurnController raises NotifyAgentActivated right after OnStartingTurn
    /// returns, and a turn already over by then would confuse every listener of that event — the hold comes
    /// first for this reason too. SignalTurnEnd sits in a finally, like in EnemyTurnDriver, so the turn always
    /// ends, even when the broadside throws.
    /// </summary>
    private async Awaitable RunTurnAsync(CancellationToken token, bool runBroadside)
    {
        try
        {
            await Awaitable.WaitForSecondsAsync(_turnHoldSeconds, token);

            if (runBroadside && _broadside != null)
                await _broadside.ExecuteBroadsideAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Destroyed mid-turn (scene unloaded): the turn loop is being cancelled too.
            return;
        }
        catch (Exception e)
        {
            // The turn is fire-and-forget: without this a broadside failure would vanish silently.
            Debug.LogException(e, this);
        }
        finally
        {
            if (!token.IsCancellationRequested) _currentTurnState?.SignalTurnEnd();
        }
    }
}
