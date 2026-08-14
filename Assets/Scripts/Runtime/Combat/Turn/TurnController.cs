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
    [Tooltip("Se assegnato, il game loop attende il gate della sequenza di intro prima di partire. Lasciare vuoto nelle scene senza intro.")]
    [SerializeField] private CombatIntroStateSO _introState;

    [Header("Combat Outcome")]
    [Tooltip("Se assegnato, il game loop smette di avviare nuovi turni quando il combattimento è risolto. Lasciare vuoto nelle scene senza esito: il gate non introduce alcun comportamento.")]
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    // Spostati da OnEnable: Unity chiama tutti gli Awake prima di tutti gli OnEnable degli oggetti
    // presenti al load. Se questi Clear() restassero in OnEnable, funzionerebbero solo grazie
    // all'await NextFrameAsync qui sotto che rimanda l'avvio del loop — ma qualunque ITurnAgent il
    // cui OnEnable (e quindi OnAgentJoin) girasse PRIMA di quello del TurnController verrebbe
    // cancellato dalla coda da questo stesso Clear(). Spostandoli in Awake l'ordine diventa
    // deterministico invece che dipendente dall'ordine dei sibling in gerarchia.
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

            // Gate della sequenza di intro: se assente o già passante, non introduce ritardo.
            if (_introState != null) await _introState.WaitUntilCombatReadyAsync(token);

            while (!token.IsCancellationRequested)
            {
                // Gate dell'esito: a combattimento risolto non si avviano più nuovi turni, ma la
                // scena resta viva (animazioni, camera, VFX continuano) esattamente come col gate
                // dell'intro qui sopra. Protegge dalla sconfitta: senza questo, in coda restano solo
                // nemici che continuerebbero a giocare turni all'infinito cercando bersagli inesistenti.
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
