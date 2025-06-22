using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ShipController : MonoBehaviour
{

    [Header("Map Tags")]
    [SerializeField] private string _modelsMapTag;
    [SerializeField] private string _floorMapTag;
    [SerializeField] private string _collisionMapTag;

    [Header("Tiles")]
    [SerializeField] private Tile _hoveredTile;
    
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
        InitializeFloor(_floorMap, _collisionMap, _hoveredTile);
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) 
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Physics.Raycast(mouseRay, out RaycastHit hit);
            Vector3Int renderingMapCell = _shipGrid.WorldToCell(hit.point);


            if (_collisionMap.cellBounds.Contains(renderingMapCell) && !_collisionMap.HasTile(renderingMapCell))
            {
                Debug.Log($"Cella: ({renderingMapCell.x}, {renderingMapCell.y}, {renderingMapCell.z}) valida!");
            }
            else 
            {
                Debug.Log($"Cella: ({renderingMapCell.x}, {renderingMapCell.y}, {renderingMapCell.z}) fuori bordo");
            }

            _modelsMap.SetTile(renderingMapCell, _hoveredTile);

            RemoveTileAfterDelay(500, renderingMapCell, _modelsMap);
        }

    }

    private void InitializeFloor(Tilemap toBeFilled, Tilemap boundTileMap, Tile tileToUse) 
    {
        toBeFilled.ClearAllTiles();
        boundTileMap.CompressBounds();

        HashSet<Vector3Int> innerArea = GridUtils.FindInnerArea(boundTileMap, new Vector3Int(0, 0 ,0));

        foreach (Vector3Int inBoundPos in innerArea) 
        {
            toBeFilled.SetTile(inBoundPos, tileToUse);
        }
    }

    private async void RemoveTileAfterDelay(int delay, Vector3Int tilePosition, Tilemap renderingMap) 
        {
            await Task.Delay(delay);

            renderingMap.SetTile(tilePosition, null);
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

}
