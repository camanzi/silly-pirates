using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ShipController : MonoBehaviour
{

    [Header("Map Tags")]
    [SerializeField] private string _modelsMapTag;
    [SerializeField] private string _floorMapTag;
    [SerializeField] private string _collisionMapTag;

    [Header("Tiles")]
    [SerializeField] private Tile _defaultFloorTile;
    [SerializeField] private Tile _hoverFloorTile;
    [SerializeField] private Tile _clickFloorTile;
    
    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;
    private Tilemap _collisionMap;

    private void Awake()
    {
        _shipGrid = GetComponentInChildren<Grid>();
        
        _modelsMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _modelsMapTag);
        _floorMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _floorMapTag);
        _collisionMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _collisionMapTag);
    }

    private void Start()
    {
        InitializeFloor(_floorMap, _collisionMap, _defaultFloorTile);
    }

    private void InitializeFloor(Tilemap toBeFilled, Tilemap boundTileMap, Tile tileToUse) 
    {
        toBeFilled.ClearAllTiles();
        boundTileMap.CompressBounds();

        HashSet<Vector3Int> innerArea = GridUtils.FindInnerArea(boundTileMap);

        foreach (Vector3Int inBoundPos in innerArea) 
        {
            toBeFilled.SetTile(inBoundPos, tileToUse);
        }
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
        _floorMap.SetTile(cellPosition, _clickFloorTile);
        ChangeTileAfterDelay(500, cellPosition, _floorMap, _defaultFloorTile);
    }

    public void HandleCellHovered(Vector3Int cellPosition)
    {
        ChangeTileAfterDelay(0, cellPosition, _floorMap, _hoverFloorTile);
    }

    public void HandleCellExited(Vector3Int cellPosition)
    {
        ChangeTileAfterDelay(0, cellPosition, _floorMap, _defaultFloorTile);
    }
}
