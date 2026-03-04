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
    [SerializeField] private Tile _hoverFloorTile;
    [SerializeField] private Tile _clickFloorTile;
    
    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;
    private Tilemap _previewMap;

    private Dictionary<Vector3Int, Tile> _previousTileSet = new Dictionary<Vector3Int, Tile>();

    private List<Vector3Int> previousPath;

    private void Awake()
    {
        previousPath = new List<Vector3Int>();
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

    public void HandleCellClicked(Vector3Int cellPosition)
    {
        CacheTile(cellPosition, _previewMap.GetTile<Tile>(cellPosition));

         _previewMap.SetTile(cellPosition, _clickFloorTile);

        _ = ChangeTileAfterDelay(500, cellPosition, _previewMap, RemoveCachedTile(cellPosition));
    }

    public void HandleCellHovered(Vector3Int cellPosition)
    {
        CacheTile(cellPosition, _previewMap.GetTile<Tile>(cellPosition));

        _ = ChangeTileAfterDelay(0, cellPosition, _previewMap, _hoverFloorTile);
    }

    public void HandleCellExited(Vector3Int cellPosition)
    {
        _ = ChangeTileAfterDelay(0, cellPosition, _previewMap, RemoveCachedTile(cellPosition));
    }

    public void HighlightPath(List<Vector3Int> path)
    {
        foreach (Vector3Int node in previousPath)
        {
            // Rimuovo la selezione precedente
            _ = ChangeTileAfterDelay(0, node, _previewMap, _defaultFloorTile);
        }
        foreach (Vector3Int node in path)
        {
            // Attivo la colorazione per i nodi
            _ = ChangeTileAfterDelay(0, node, _previewMap, _hoverFloorTile);
        }
        previousPath = path;
    }

    private async Awaitable ChangeTileAfterDelay(int delay, Vector3Int tilePosition, Tilemap renderingMap, Tile newTile = null) 
    {
        if (delay > 0)
        {
            await Awaitable.WaitForSecondsAsync(delay);
            
        }

        renderingMap.SetTile(tilePosition, newTile);
    }

    private void CacheTile(Vector3Int cellPosition, Tile tile)
    {
        _previousTileSet.TryAdd(cellPosition, tile);
    }

    private Tile RemoveCachedTile(Vector3Int cellPosition)
    {
        _previousTileSet.Remove(cellPosition, out Tile removedTile);

        return removedTile;
    }
}
