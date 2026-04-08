using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private HighlightGridEventChannel _highlightCellsEventChannel;
    [SerializeField] private HighlightFreeAimEventChannel _targetTransformEventChannel;

    [Header("Turn Managments")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private TurnStateSO _currentTurnState;

    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    // FIXME Later Idealmente quando INIZIA un fight bisogna chiamare questa cosa
    void OnEnable()
    {
        _turnOrderData.Clear();
        _currentTurnState.Clear();
    }

    #region ABILITY PREVIEW
    // Attenzione forse la gestione corretta non dovrebbe essere qui, andrebbe spostata in un qualcosa di dedicato alla visualizzazione delle preview
    public void DrawAbilityPreview(TargetingData targetingData, AbilityBase ability, IInteractableElement caster)
    {
        AbilityPreviewData data = ability.GetPreviewData(caster, targetingData);
        bool canExecute = ability.CanExecute(caster, targetingData);

        if (ability.ShowTrajectory)
            _targetTransformEventChannel.RaiseEvent(new HighlightFreeAimPayload(caster, canExecute, data.FreeAimTargets, targetingData.worldPosition, ability.TrajectoryConfigData));
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload(data.AffectedCells, canExecute));
    }

    public void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(HighlightGridPayload.Empty);
        _targetTransformEventChannel.RaiseEvent(HighlightFreeAimPayload.Empty);
    }
    #endregion

    #region COMMANDS HANDLING
    public void AddCommand(ICommand command) => _commandQueue.Enqueue(command);

    public async Awaitable ProcessQueueAsync() {
        if (_isProcessing) return;
        _isProcessing = true;

        while (_commandQueue.Count > 0) {
            ICommand cmd = _commandQueue.Dequeue();
            await cmd.ExecuteAsync(); 
        }

        _isProcessing = false;
    }
    #endregion

    public async Awaitable RunGameLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            ITurnAgent nextEntity = _turnOrderData.TurnQueue[0].Agent;
            
            _currentTurnState.SetActiveCharacter(nextEntity);

            await _currentTurnState.WaitUntilTurnFinished();

            _turnOrderData.CompleteActiveTurn();
        }
    }

    public void OnAgentJoin(ITurnAgent agent) => _turnOrderData.AddEntity(agent);
    public void OnAgentLeave(ITurnAgent agent)
    {
        _turnOrderData.RemoveEntity(agent);
        
        if (_currentTurnState.ActiveAgent == agent)
        {
            _currentTurnState.SignalTurnEnd();
        }
    }
}
