using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GridElement : MonoBehaviour
{

    [Header("Configs")]
    [SerializeField] private LayerMask floorGridLayer;

    public Vector3Int gridPosition => _gridPosition;
    public Vector3 worldPosition => _floorTilemap.CellToWorld(_gridPosition);
    protected Tilemap _floorTilemap;
    protected Vector3Int _gridPosition;
    
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
            _gridPosition = _floorTilemap.WorldToCell(hitInfo.point);
        }
    }
}
