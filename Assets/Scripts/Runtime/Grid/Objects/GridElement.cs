using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GridElement : MonoBehaviour
{

    [Header("Configs")]
    [SerializeField] private LayerMask floorGridLayer;

    public Vector3Int gridPosition { get; set; }
    public Vector3 worldPosition => _floorTilemap.CellToWorld(gridPosition);
    public Tilemap activeTilemap =>_floorTilemap;
    protected Tilemap _floorTilemap;
    
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
