using UnityEngine;
using UnityEngine.Tilemaps;

public class GridElement : MonoBehaviour
{

    [Header("Configs")]
    [SerializeField] private LayerMask floorGridLayer;
    [SerializeField] private GridStateDataSO _gridStateData;

    public Vector3Int gridPosition {
        get { return _gridPosition; }
        set
        {
            _gridStateData.UnregisterOccupancy(_gridPosition, this);
            OnGridPositionChanged(_gridPosition, value);
            _gridPosition = value;
            _gridStateData.RegisterOccupancy(gridPosition, this);
        }
    }

    protected virtual void OnGridPositionChanged(Vector3Int prevPosition, Vector3Int newPosition) { }

    public Vector3 worldPosition => _floorTilemap.CellToWorld(gridPosition);
    public Tilemap activeTilemap =>_floorTilemap;
    protected Tilemap _floorTilemap;

    private Vector3Int _gridPosition;
    
    protected virtual void Awake() { }

    protected virtual void OnEnable()
    {
        InitializePosition();
    }

    protected virtual void OnDisable()
    {
        // Without this, unloading the scene leaves the destroyed GridElement inside
        // GridStateDataSO._occupiedCells: IsOccupied() is a dictionary lookup, not a Unity null check, so
        // in the next combat that cell reads as occupied forever and pathfinding dies. Subclasses that
        // override OnDisable MUST call base.OnDisable() so they do not silence this unregister.
        if (_gridStateData != null)
            _gridStateData.UnregisterOccupancy(_gridPosition, this);
    }

    private void InitializePosition()
    {
        Ray ray = new Ray(transform.position + (transform.up * 0.5f), -transform.up);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 5f, floorGridLayer))
        {
            _floorTilemap = hitInfo.transform.GetComponent<Tilemap>();
            // Set directly to avoid triggering OnGridPositionChanged before the scene is ready
            var pos = _floorTilemap.WorldToCell(hitInfo.point);
            _gridStateData.UnregisterOccupancy(_gridPosition, this);
            _gridPosition = pos;
            _gridStateData.RegisterOccupancy(_gridPosition, this);
        }
        else
        {
            // This is never an intended condition: with no tilemap the element registers no occupancy
            // (pathfinding treats its cell as free) and every ability reading activeTilemap blows up with
            // a NullReference much later, far away from the real cause.
            Debug.LogError(
                $"[{nameof(GridElement)}] '{name}': no floor found below {transform.position} " +
                $"(layer '{floorGridLayer.value}'). The element is left with no tilemap and no occupancy.", this);
        }
    }
}
