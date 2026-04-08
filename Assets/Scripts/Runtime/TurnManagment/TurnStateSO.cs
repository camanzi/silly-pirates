using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentTurnState", menuName = "Combat/Turn System/Turn State")]
public class TurnStateSO : ScriptableObject
{
    [SerializeField] private TurnAgentEventChannel _onAgentActivated;
    public TurnAgentEventChannel OnAgentActivated => _onAgentActivated;
    private ITurnAgent _activeAgent;
    
    private AwaitableCompletionSource _turnTaskSource;

    public ITurnAgent ActiveAgent => _activeAgent;
    public bool IsPlayerTurn => true;

    public void SetActiveCharacter(ITurnAgent agent)
    {
        _activeAgent = agent;
        _turnTaskSource = new AwaitableCompletionSource();

        _onAgentActivated?.RaiseEvent(agent);
    }

    public void SignalTurnEnd() => _turnTaskSource?.SetResult();

    public async Awaitable WaitUntilTurnFinished() => await _turnTaskSource.Awaitable;

    public void Clear() => _activeAgent = null;
}