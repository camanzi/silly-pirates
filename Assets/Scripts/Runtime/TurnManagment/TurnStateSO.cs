using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentTurnState", menuName = "Combat/Turn System/Turn State")]
public class TurnStateSO : ScriptableObject, ICombatSessionResettable
{
    [SerializeField] private TurnAgentEventChannel _onAgentActivated;
    public TurnAgentEventChannel OnAgentActivated => _onAgentActivated;

    public ITurnAgent ActiveAgent => _activeAgent;
    public bool IsPlayerTurn => _isPlayerTurn;
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

    public async Awaitable WaitUntilTurnFinished() => await _turnTaskSource.Awaitable;

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