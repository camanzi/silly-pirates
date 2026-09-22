using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentTurnState", menuName = "Combat/Turn System/Turn State")]
public class TurnStateSO : ScriptableObject, ICombatSessionResettable
{
    [SerializeField] private TurnAgentEventChannel _onAgentActivated;
    // internal setter (test seam): the raise is null-conditional, so without a channel a test cannot
    // observe NotifyAgentActivated at all.
    public TurnAgentEventChannel OnAgentActivated { get => _onAgentActivated; internal set => _onAgentActivated = value; }

    public ITurnAgent ActiveAgent => _activeAgent;
    public bool IsPlayerTurn => _isPlayerTurn;

    /// <summary>
    /// True while a non-player agent holds the turn. Deliberately not the same as !IsPlayerTurn: that one is
    /// also true with no turn running at all — _isPlayerTurn starts false and Clear() puts it back — so a
    /// caller gating on it would treat the moments before the first turn, and after a return to the menu, as
    /// an enemy's turn.
    /// </summary>
    public bool IsEnemyTurn => _activeAgent != null && !_isPlayerTurn;
    private ITurnAgent _activeAgent;
    private AwaitableCompletionSource _turnTaskSource;
    private bool _isPlayerTurn = false;
    private int _currentActionIndex;

    public int CurrentActionIndex => _currentActionIndex;

    public void SetActiveCharacter(ITurnAgent agent, int actionIndex = 0)
    {
        _activeAgent = agent;
        _currentActionIndex = actionIndex;
        _turnTaskSource = new AwaitableCompletionSource();

        _isPlayerTurn = _activeAgent.CompareTag("Player");
    }

    public void NotifyAgentActivated() => _onAgentActivated?.RaiseEvent(_activeAgent);

    public void SignalTurnEnd() => _turnTaskSource?.SetResult();

    /// <summary>
    /// Awaits the end of the active turn.
    /// Fails fast rather than NullReferencing: awaiting with no active turn is always a caller bug
    /// (the turn loop asked to wait before SetActiveCharacter, or after Clear()/ResetForNewCombat()),
    /// and the bare NRE this used to throw pointed at this line instead of at the caller.
    /// </summary>
    public async Awaitable WaitUntilTurnFinished()
    {
        if (_turnTaskSource == null)
            throw new InvalidOperationException("No active turn: SetActiveCharacter was never called, or Clear() ran.");

        await _turnTaskSource.Awaitable;
    }

    // It used to clear only _activeAgent. _currentActionIndex survived the reset and was read by
    // TurnOrderController.RebuildDisplayList holding the previous combat's value; _turnTaskSource and
    // _isPlayerTurn stayed stale, ready to confuse the first new turn.
    public void Clear()
    {
        _activeAgent = null;
        _turnTaskSource = null;
        _isPlayerTurn = false;
        _currentActionIndex = 0;
    }

    public void ResetForNewCombat() => Clear();
}