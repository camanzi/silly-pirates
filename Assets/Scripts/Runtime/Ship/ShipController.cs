using System.Collections.Generic;
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
    [SerializeField] private Tile _hoverFloorAllowTile;
    [SerializeField] private Tile _hoverFloorNotAllowTile;
    [SerializeField] private Tile _clickFloorTile;
    
    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;
    private Tilemap _previewMap;

    private Dictionary<Vector3Int, Tile> _previousTileSet = new Dictionary<Vector3Int, Tile>();

    private ICollection<Vector3Int> previousHighlight;

    private void Awake()
    {
        previousHighlight = new List<Vector3Int>();
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

    public void HighlightPath(HighlightCellsPayload payload)
    {
        foreach (Vector3Int node in previousHighlight ?? new List<Vector3Int>())
        {
            _ = ChangeTileAfterDelay(0, node, _previewMap, _defaultFloorTile);
        }

        foreach (Vector3Int node in payload.cells ?? new List<Vector3Int>())
        {
            _ = ChangeTileAfterDelay(0, node, _previewMap, payload.isValidHighlight ? _hoverFloorAllowTile : _hoverFloorNotAllowTile);
        }

        previousHighlight = payload.cells;
    }

    private async Awaitable ChangeTileAfterDelay(int delay, Vector3Int tilePosition, Tilemap renderingMap, Tile newTile = null) 
    {
        if (delay > 0)
        {
            await Awaitable.WaitForSecondsAsync(delay);
        }

        renderingMap.SetTile(tilePosition, newTile);
    }
}
