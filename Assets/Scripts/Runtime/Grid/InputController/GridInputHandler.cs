using UnityEngine;
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
    [SerializeField] private LayerMask _uiLayer;
    
    [Header("Movement Settings")]
    [SerializeField] private float _mouseMoveThreshold = 1.0f;

    [Header("event Channels")]
    [SerializeField] private TargetingDataEventChannel _onPointerMoved;
    [SerializeField] private TargetingDataEventChannel _onPointerClicked;    
    private Vector3Int? _lastHoveredCell;
    private Vector2 _lastScreenPosition;
    
    private Camera _mainCamera;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    private void Update()
    {
        TargetingData data = GetTargetingData();
    
        if (HasPointerMoved() || data.cellPosition != _lastHoveredCell)
        {
            _lastHoveredCell = data.cellPosition;
            _onPointerMoved.RaiseEvent(data);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _onPointerClicked.RaiseEvent(data);
        }
    }
    
    private TargetingData GetTargetingData()
    {
        if (IsPointerOverUI() || Mouse.current == null) return TargetingData.Empty;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 finalWorldPos = hit.point;
            ITargettable finalTarget = null;

            if (hit.collider.TryGetComponent(out ITargettable target))
            {
                finalTarget = target;
                finalWorldPos = hit.collider.transform.position;
            }

            Vector3Int cellPos = _grid.WorldToCell(finalWorldPos);
            bool isValidCell = _interactableTilemap.HasTile(cellPos);

            return new TargetingData(
                worldPosition: finalWorldPos, 
                cellPosition: cellPos, 
                isOverValidGrid: isValidCell,
                selectedTarget: finalTarget
            );
        }

        return TargetingData.Empty;
    }

    private bool HasPointerMoved()
    {
        if (Mouse.current == null) return false;

        Vector2 currentScreenPos = Mouse.current.position.ReadValue();
        
        float distance = Vector2.Distance(currentScreenPos, _lastScreenPosition);

        if (distance >= _mouseMoveThreshold)
        {
            _lastScreenPosition = currentScreenPos;
            return true;
        }

        return false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (!EventSystem.current.IsPointerOverGameObject()) return false;

        GameObject currentObj = EventSystem.current.currentSelectedGameObject;
        
        if (currentObj == null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Mouse.current.position.ReadValue();
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                if (result.gameObject.layer == _uiLayer) 
                    return true;
            }
            return false;
        }

        return currentObj.layer == _uiLayer;
    }
}