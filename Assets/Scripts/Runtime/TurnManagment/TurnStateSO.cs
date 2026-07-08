using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentTurnState", menuName = "Combat/Turn System/Turn State")]
public class TurnStateSO : ScriptableObject
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

    public void Clear() => _activeAgent = null;
}