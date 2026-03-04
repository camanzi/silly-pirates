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
    }

    public void HandleCellHovered(Vector3Int cellPosition)
    {
        CacheTile(cellPosition, _floorMap.GetTile<Tile>(cellPosition));
    }

    public void HandleCellExited(Vector3Int cellPosition)
    {
    }

    public void HighlightPath(List<Vector3Int> path)
    {
        foreach (Vector3Int node in previousPath)
        {
            // Rimuovo la selezione precedente
        }
        foreach (Vector3Int node in path)
        {
            // Attivo la colorazione per i nodi
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
