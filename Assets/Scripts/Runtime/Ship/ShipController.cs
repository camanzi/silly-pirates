using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ShipController : MonoBehaviour
{

    [Header("Map Tags")]
    [SerializeField] private string _modelsMapTag;
    [SerializeField] private string _floorMapTag;
    [SerializeField] private string _previewMapTag;

    [Header("Tiles")]
    [SerializeField] private Tile _defaultFloorTile;
    [SerializeField] private Tile _inRangePreviewTile;
    [SerializeField] private Tile _hoverFloorAllowTile;
    [SerializeField] private Tile _hoverFloorNotAllowTile;
    
    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;
    private Tilemap _previewMap;

    private ICollection<Vector3> _cacheedHighlight;

    private void Awake()
    {
        _cacheedHighlight = new List<Vector3>();
        _shipGrid = GetComponentInChildren<Grid>();
        
        _modelsMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _modelsMapTag);
        _floorMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _floorMapTag);
        _previewMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _previewMapTag);
    }

    private T FindChildByTag<T>(GameObject parent, string tag) 
    {
        T result = default(T);

        for (int i = 0; i < parent.transform.childCount; i++) 
        {
            Transform child = parent.transform.GetChild(i);
            if (child.tag.Equals(tag)) 
            {
                result = child.GetComponent<T>();
            }
        }

        return result;
    }

    public void HighlightPath(HighlightGridPayload payload)
    {
        foreach (Vector3 node in _cacheedHighlight ?? new List<Vector3>())
        {
            ChangeTile(Vector3Int.FloorToInt(node), _previewMap, _defaultFloorTile);
        }

        foreach (Vector3 node in payload.InteractionArea ?? new List<Vector3>())
        {
            ChangeTile(Vector3Int.FloorToInt(node), _previewMap, _inRangePreviewTile);
        }

        foreach (Vector3 node in payload.AffectedCells ?? new List<Vector3>())
        {
            ChangeTile(Vector3Int.FloorToInt(node), _previewMap, payload.IsValidHighlight ? _hoverFloorAllowTile : _hoverFloorNotAllowTile);
        }

        _cacheedHighlight.Clear();
        _cacheedHighlight.AddRange(payload.AffectedCells);
        _cacheedHighlight.AddRange(payload.InteractionArea);
    }

    private async Awaitable ChangeTile(Vector3Int tilePosition, Tilemap renderingMap, Tile newTile, int delay) 
    {
        await Awaitable.WaitForSecondsAsync(delay);
        ChangeTile(tilePosition, renderingMap, newTile);
    }

    private void ChangeTile(Vector3Int tilePosition, Tilemap renderingMap, Tile newTile = null)
    {
        renderingMap.SetTile(tilePosition, newTile);
    }
}
