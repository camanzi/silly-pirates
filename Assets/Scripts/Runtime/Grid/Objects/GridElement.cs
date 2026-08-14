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
        // Senza questo, scaricare la scena lascia il GridElement distrutto dentro
        // GridStateDataSO._occupiedCells: IsOccupied() è una lookup su dizionario, non un null check
        // Unity, quindi al combattimento successivo la cella risulta occupata per sempre e il
        // pathfinding muore. Le sottoclassi che fanno override di OnDisable DEVONO chiamare
        // base.OnDisable() per non silenziare questo unregister.
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
            // Non è mai una condizione voluta: senza tilemap l'elemento non registra occupancy
            // (il pathfinding considera libera la sua cella) e ogni ability che legge activeTilemap
            // esplode con una NullReference molto più tardi, lontano dalla vera causa.
            Debug.LogError(
                $"[{nameof(GridElement)}] '{name}': nessun pavimento trovato sotto {transform.position} " +
                $"(layer '{floorGridLayer.value}'). L'elemento resta senza tilemap e senza occupancy.", this);
        }
    }
}
