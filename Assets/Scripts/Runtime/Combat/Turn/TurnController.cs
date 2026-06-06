using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TurnController : MonoBehaviour
{
    [Header("Turn Managments")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private TurnStateSO _currentTurnState;
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;
    [SerializeField] private CommandQueueSO _commandQueue;

    protected async Awaitable OnEnable()
    {
        _turnOrderData.Clear();
        _currentTurnState.Clear();
        _commandQueue.Clear();

        await Awaitable.NextFrameAsync();
        _ = RunGameLoop(destroyCancellationToken);
    }

    #region TURN AGENT HANDLING & CORE GAME LOOP
    public async Awaitable RunGameLoop(CancellationToken token)
    {
        try 
        {
            await Awaitable.NextFrameAsync(token);

            while (!token.IsCancellationRequested)
            {
                if (_turnOrderData.TurnQueue.Count == 0)
                {
                    await Awaitable.NextFrameAsync(token);
                    continue;
                }

                ITurnAgent nextEntity = _turnOrderData.TurnQueue[0].Agent;

                _turnOrderData.StartActiveTurn();

                int actionsPerTurn = nextEntity.AgentData.ActionsPerTurn;
                bool agentStillActive = true;

                for (int action = 0; action < actionsPerTurn; action++)
                {
                    _currentTurnState.SetActiveCharacter(nextEntity, action);
                    if (action == 0) nextEntity.OnStartingTurn();
                    else nextEntity.OnContinuingTurn();

                    await _currentTurnState.WaitUntilTurnFinished();

                    agentStillActive = _turnOrderData.TurnQueue.Count > 0
                                       && _turnOrderData.TurnQueue[0].Agent == nextEntity;
                    if (!agentStillActive) break;
                }

                _onAnyTurnEnded?.RaiseEvent(nextEntity);
                if (agentStillActive)
                    _turnOrderData.CompleteActiveTurn();
            }
        } 
        catch (OperationCanceledException)
        {
            Debug.Log("Turn Loop cancellato correttamente.");
        }
    }

    public void OnAgentJoin(ITurnAgent agent) => _turnOrderData.AddEntity(agent);

    public void OnAgilityChanged(AgentAVDeltaPayload payload) => _turnOrderData.AdjustAgentAV(payload.Agent, payload.AVDelta);

    public void OnAgentLeave(ITurnAgent agent)
    {
        _turnOrderData.RemoveEntity(agent);
        
        if (_currentTurnState.ActiveAgent == agent)
        {
            _currentTurnState.SignalTurnEnd();
        }
    }
    #endregion
}
