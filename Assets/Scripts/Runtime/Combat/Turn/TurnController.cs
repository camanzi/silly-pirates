using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private HighlightEventChannel _highlightCellsEventChannel;
    [SerializeField] private HighlightEventChannel _targetTransformEventChannel;

    private SelectedAbilityPayload? _selectedAbilityPayload;

    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    public void UpdateSelectedAbility(SelectedAbilityPayload? payload)
    {
        _selectedAbilityPayload = payload;
    }

    public void OnPointerMoved(TargetingData data)
    {
        if (!_selectedAbilityPayload.HasValue) return;

        if (!data.isOverValidGrid && !_selectedAbilityPayload.Value.ability.IsFreeAim)
        {
            ClearPreview();
            return;
        }

        DrawPreview(data);
    }

    public void OnPointerClicked(TargetingData targetingData)
    {
        if (!_selectedAbilityPayload.HasValue) return;

        GridElement caster = _selectedAbilityPayload.Value.caster;
        AbilityBase ability = _selectedAbilityPayload.Value.ability;
        Vector3 target = ability.IsFreeAim ? targetingData.worldPosition : targetingData.cellPosition;

        if (!ability.CanExecute(_selectedAbilityPayload.Value.caster, target)) return;

        // I target in area ora sono vuoti... piú avanti capire come calcolari
        ICommand selectedAbilityCommand = ability.CreateCommand(caster, target, new List<GridElement>());
        AddCommand(selectedAbilityCommand);

        ClearPreview();
        
        // Per il momento faccio partire subito il ProcessQueueAsync, in futuro potrebbe essere da spostare
        _ = ProcessQueueAsync();
    }

    private void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(HighlightPayload.Empty);
        _targetTransformEventChannel.RaiseEvent(HighlightPayload.Empty);
    }

    private void DrawPreview(TargetingData targetingData)
    {
        AbilityBase ability = _selectedAbilityPayload.Value.ability;
        GridElement caster = _selectedAbilityPayload.Value.caster;

        AbilityPreviewData data = ability.GetPreviewData(ability.IsFreeAim ? targetingData.worldPosition : targetingData.cellPosition, caster);
        bool canExecute = ability.CanExecute(caster, ability.IsFreeAim ? targetingData.worldPosition : targetingData.cellPosition);

        _highlightCellsEventChannel.RaiseEvent(new HighlightPayload(data.affectedCells, canExecute));
        _targetTransformEventChannel.RaiseEvent(new HighlightPayload(data.freeAimTargets, canExecute, caster.transform.position));
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
