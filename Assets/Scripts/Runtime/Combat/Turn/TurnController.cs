using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private Vector3IntListEventChannel highlightPathEventChannel;

    private InteractableGridElement _selectedGridElement;

    public void DisablePathPreview(InteractableGridElement deselectedCharacter)
    {
        _selectedGridElement = null;
    }

    public void EnablePathPreview(InteractableGridElement selectedCharacter)
    {
        _selectedGridElement = selectedCharacter;
    }

    public void DrawPreviewPath(Vector3Int endPosition)
    {
        if (_selectedGridElement == null) return;

        List<Vector3Int> path = PathFindingUtils.FindPath(_selectedGridElement.gridPosition, endPosition, (pos) => true);        
        highlightPathEventChannel.RaiseEvent(path);
    }

    public void HidePath()
    {
        highlightPathEventChannel.RaiseEvent(new List<Vector3Int>());
    }

    public void MoveToPosition(Vector3Int endPosition)
    {
        if (_selectedGridElement == null) return;

        _selectedGridElement.ExecuteAction(endPosition);
    }
}
