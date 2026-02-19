using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ShipController : MonoBehaviour
{

    [Header("Map Tags")]
    [SerializeField] private string _modelsMapTag;
    [SerializeField] private string _floorMapTag;

    [Header("Tiles")]
    [SerializeField] private Tile _defaultFloorTile;
    [SerializeField] private Tile _hoverFloorTile;
    [SerializeField] private Tile _clickFloorTile;
    
    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;

    private Dictionary<Vector3Int, Tile> _previousTileSet = new Dictionary<Vector3Int, Tile>();

    private List<Vector3Int> previousPath;

    private void Awake()
    {
        previousPath = new List<Vector3Int>();
        _shipGrid = GetComponentInChildren<Grid>();
        
        _modelsMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _modelsMapTag);
        _floorMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _floorMapTag);
    }

    private async void ChangeTileAfterDelay(int delay, Vector3Int tilePosition, Tilemap renderingMap, Tile newTile = null) 
    {
        await Task.Delay(delay);

        renderingMap.SetTile(tilePosition, newTile);
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
        CacheTile(cellPosition, _floorMap.GetTile<Tile>(cellPosition));

        _floorMap.SetTile(cellPosition, _clickFloorTile);

        ChangeTileAfterDelay(500, cellPosition, _floorMap, RemoveCachedTile(cellPosition));
    }

    public void HandleCellHovered(Vector3Int cellPosition)
    {
        CacheTile(cellPosition, _floorMap.GetTile<Tile>(cellPosition));

        ChangeTileAfterDelay(0, cellPosition, _floorMap, _hoverFloorTile);
    }

    public void HandleCellExited(Vector3Int cellPosition)
    {
        ChangeTileAfterDelay(0, cellPosition, _floorMap, RemoveCachedTile(cellPosition));
    }

    public void HighlightPath(List<Vector3Int> path)
    {
        foreach (Vector3Int node in previousPath)
        {
            ChangeTileAfterDelay(0, node, _floorMap, _defaultFloorTile);
        }
        foreach (Vector3Int node in path)
        {
            ChangeTileAfterDelay(0, node, _floorMap, _hoverFloorTile);
        }
        previousPath = path;
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
