using UnityEngine;
using UnityEngine.EventSystems;
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
            _gridPosition = value;
            _gridStateData.RegisterOccupancy(gridPosition, this);
        }
        }
    public Vector3 worldPosition => _floorTilemap.CellToWorld(gridPosition);
    public Tilemap activeTilemap =>_floorTilemap;
    protected Tilemap _floorTilemap;

    private Vector3Int _gridPosition;
    
    protected virtual void Awake()
    {
        
    }

    protected virtual void OnEnable()
    {
        InitializePosition();
    }

    private void InitializePosition()
    {
        Ray ray = new Ray(transform.position + (transform.up * 0.5f), -transform.up);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 5f, floorGridLayer))
        {
            _floorTilemap = hitInfo.transform.GetComponent<Tilemap>();
            gridPosition = _floorTilemap.WorldToCell(hitInfo.point);
        }
    }
}
