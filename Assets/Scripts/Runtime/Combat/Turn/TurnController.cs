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

    public void DrawAbilityPreview(TargetingData targetingData, AbilityBase ability, GridElement caster)
    {
        Vector3 targetPos = ability.IsFreeAim ? targetingData.worldPosition : targetingData.cellPosition;
        AbilityPreviewData data = ability.GetPreviewData(targetPos, caster);
        bool canExecute = ability.CanExecute(caster, targetPos);

        _highlightCellsEventChannel.RaiseEvent(new HighlightPayload(data.affectedCells, canExecute));
        _targetTransformEventChannel.RaiseEvent(new HighlightPayload(data.freeAimTargets, canExecute, caster.transform.position));
    }

    // Questo conferma l'abilitá, ma dovrebbe essere lo state del combat a dare il via a questo giro, non il TurnController
    public void OnPointerClicked(TargetingData targetingData)
    {
        // if (!_selectedAbilityPayload.HasValue) return;

        // GridElement caster = _selectedAbilityPayload.Value.caster;
        // AbilityBase ability = _selectedAbilityPayload.Value.ability;
        // Vector3 target = ability.IsFreeAim ? targetingData.worldPosition : targetingData.cellPosition;

        // if (!ability.CanExecute(_selectedAbilityPayload.Value.caster, target)) return;

        // // I target in area ora sono vuoti... piú avanti capire come calcolari
        // ICommand selectedAbilityCommand = ability.CreateCommand(caster, target, new List<GridElement>());
        // AddCommand(selectedAbilityCommand);

        // ClearPreview();
        
        // // Per il momento faccio partire subito il ProcessQueueAsync, in futuro potrebbe essere da spostare
        // _ = ProcessQueueAsync();
    }

    // Attenzione forse la gestione corretta non dovrebbe essere qui, andrebbe spostata in un qualcosa di dedicato alla visualizzazione delle preview
    public void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(HighlightPayload.Empty);
        _targetTransformEventChannel.RaiseEvent(HighlightPayload.Empty);
    }

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
