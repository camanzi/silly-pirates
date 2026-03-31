using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private HighlightEventChannel _highlightCellsEventChannel;
    [SerializeField] private HighlightEventChannel _targetTransformEventChannel;

    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    #region ABILITY PREVIEW
    // Attenzione forse la gestione corretta non dovrebbe essere qui, andrebbe spostata in un qualcosa di dedicato alla visualizzazione delle preview
    public void DrawAbilityPreview(TargetingData targetingData, AbilityBase ability, IInteractableElement caster)
    {
        Vector3 targetPos = ability.isFreeAim ? targetingData.worldPosition : targetingData.cellPosition;
        AbilityPreviewData data = ability.GetPreviewData(targetPos, caster);
        bool canExecute = ability.CanExecute(caster, targetPos);

        _highlightCellsEventChannel.RaiseEvent(new HighlightPayload(data.affectedCells, canExecute));
        _targetTransformEventChannel.RaiseEvent(new HighlightPayload(data.freeAimTargets, canExecute, caster.Transform.position));
    }

    public void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(HighlightPayload.Empty);
        _targetTransformEventChannel.RaiseEvent(HighlightPayload.Empty);
    }
    #endregion

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
}
