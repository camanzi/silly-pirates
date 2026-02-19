using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

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

        Tilemap floorTilemap = _selectedGridElement.activeTilemap;

        HashSet<Vector3Int> walkableCache = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in floorTilemap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = floorTilemap.GetTile<TerrainTile>(pos);
            if (tile != null && tile.isWalkable)
            {
                walkableCache.Add(pos);
            }
        }
        bool includeStartingPosition = true;
        List<Vector3Int> path = PathFindingUtils.FindPath(_selectedGridElement.gridPosition, endPosition, includeStartingPosition, walkableCache, static (pos, cache) => cache.Contains(pos));        
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
