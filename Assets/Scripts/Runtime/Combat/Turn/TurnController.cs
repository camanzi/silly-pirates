using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private HighlightCellsEventChannel _highlightCellsEventChannel;

    private SelectedAbilityPayload? _selectedAbility;

    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    #region Path Preview
    public void UpdateSelectedAbility(SelectedAbilityPayload? payload)
    {
        _selectedAbility = payload;
    }

    public void DrawPreview(Vector3Int endPosition)
    {
        if (!_selectedAbility.HasValue) return;

        List<Vector3Int> affectedCells = _selectedAbility.Value.selectedAbility.GetAffectedCells(endPosition, _selectedAbility.Value.caster);
        bool canExecute = _selectedAbility.Value.selectedAbility.CanExecute(_selectedAbility.Value.caster, endPosition);

        _highlightCellsEventChannel.RaiseEvent(new HighlightCellsPayload(affectedCells, canExecute));
    } 

    public void OnCellClicked(Vector3Int target)
    {   
        if (!_selectedAbility.HasValue || !_selectedAbility.Value.selectedAbility.CanExecute(_selectedAbility.Value.caster, target)) return;

        ICommand selectedAbilityCommand = _selectedAbility.Value.selectedAbility.CreateCommand(_selectedAbility.Value.caster, target, new List<GridElement>());
        AddCommand(selectedAbilityCommand);

        _highlightCellsEventChannel.RaiseEvent(new HighlightCellsPayload());

        // Per il momento faccio partire subito il ProcessQueueAsync, in futuro potrebbe essere da spostare
        _ = ProcessQueueAsync();
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
