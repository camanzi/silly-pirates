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

    [Header("Combat Intro")]
    [Tooltip("When assigned, the game loop waits on the intro sequence's gate before starting. Leave empty in scenes with no intro.")]
    [SerializeField] private CombatIntroStateSO _introState;

    [Header("Combat Outcome")]
    [Tooltip("When assigned, the game loop stops starting new turns once the combat is resolved. Leave empty in scenes with no outcome: the gate then adds no behaviour at all.")]
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    // Moved out of OnEnable: Unity calls every Awake before every OnEnable of the objects present at
    // load. Were these Clear() calls to stay in OnEnable, they would only work thanks to the
    // await NextFrameAsync below deferring the start of the loop — but any ITurnAgent whose OnEnable
    // (and therefore OnAgentJoin) ran BEFORE the TurnController's would be wiped from the queue by this
    // very Clear(). Moving them into Awake makes the ordering deterministic rather than dependent on
    // sibling order in the hierarchy.
    private void Awake()
    {
        _turnOrderData.Clear();
        _currentTurnState.Clear();
        _commandQueue.Clear();
    }

    protected async Awaitable OnEnable()
    {
        await Awaitable.NextFrameAsync();
        _ = RunGameLoop(destroyCancellationToken);
    }

    #region TURN AGENT HANDLING & CORE GAME LOOP
    public async Awaitable RunGameLoop(CancellationToken token)
    {
        try 
        {
            await Awaitable.NextFrameAsync(token);

            // The intro sequence's gate: when absent or already open, it adds no delay.
            if (_introState != null) await _introState.WaitUntilCombatReadyAsync(token);

            while (!token.IsCancellationRequested)
            {
                // The outcome gate: once the combat is resolved no new turns are started, but the scene
                // stays alive (animations, camera and VFX keep running) exactly as with the intro gate
                // above. It guards against defeat: without it the queue would be left with enemies only,
                // playing turns forever looking for targets that no longer exist.
                if (_outcomeState != null && _outcomeState.IsCombatOver)
                {
                    await Awaitable.NextFrameAsync(token);
                    continue;
                }

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
                    _currentTurnState.NotifyAgentActivated();

                    await _currentTurnState.WaitUntilTurnFinished();

                    agentStillActive = _turnOrderData.TurnQueue.Count > 0
                                       && _turnOrderData.TurnQueue[0].Agent == nextEntity;
                    if (!agentStillActive) break;
                }

                nextEntity.OnEndingTurn();
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
