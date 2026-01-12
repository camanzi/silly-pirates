using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class GridInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _interactableTilemap;
    
    [Header("Settings")]
    [SerializeField] private LayerMask _gridLayerMask;
    
    [Header("event Channels")]
    public Vector3IntEventChannel OnCellClicked;
    public Vector3IntEventChannel OnCellHovered;
    public Vector3IntEventChannel OnCellExited;
    public Vector3IntEventChannel OnTileMapExit;
    
    private Vector3Int? _lastHoveredCell;
    
    private Camera _mainCamera;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    private void Update()
    {
        HandleHover();
        HandleClick();
    }
    
    private void HandleHover()
    {
        bool hasFoundCell = TryGetCellAtMousePosition(out Vector3Int cellPosition); 
        if (hasFoundCell && _lastHoveredCell != cellPosition)
        {
            if (_lastHoveredCell.HasValue)
            {
                OnCellExited.RaiseEvent(_lastHoveredCell.Value);
            }

            _lastHoveredCell = cellPosition;
            OnCellHovered.RaiseEvent(cellPosition);
        } else if (!hasFoundCell && _lastHoveredCell.HasValue)
        {
            OnTileMapExit.RaiseEvent(_lastHoveredCell.Value);
            _lastHoveredCell = null;
        }
    }
    
    private void HandleClick()
    {
        // FIXME poi da capire come gestirlo se si usa un controller o comunque un altro dispositivo
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (TryGetCellAtMousePosition(out Vector3Int cellPosition))
            {
                OnCellClicked.RaiseEvent(cellPosition);
            }
        }
    }
    
    private bool TryGetCellAtMousePosition(out Vector3Int cellPosition)
    {
        cellPosition = Vector3Int.zero;
        
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        if (Mouse.current == null)
            return false;
        
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);
        
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, _interactableTilemap.transform.position.y, 0));
        
        if (gridPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            cellPosition = _grid.WorldToCell(hitPoint);
            
            if (_interactableTilemap.HasTile(cellPosition))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public Vector3 GetWorldPositionOfCell(Vector3Int cellPosition)
    {
        return _grid.GetCellCenterWorld(cellPosition);
    }
}