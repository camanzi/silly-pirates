using Unity.Cinemachine;
using UnityEngine;

public class TurnController : MonoBehaviour
{
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
    }

    public void MoveToPosition(Vector3Int endPosition)
    {
        if (_selectedGridElement == null) return;

        _selectedGridElement.ExecuteAction(endPosition);
    }
}
