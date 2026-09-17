using UnityEngine;
using UnityEngine.Tilemaps;

public class GridInputHandler : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _interactableTilemap;
    
    [Header("Event Channels")]
    [SerializeField] private TargetingDataEventChannel _onPointerMoved;
    [SerializeField] private TargetingDataEventChannel _onPointerClicked;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    [Header("Interaction")]
    [Tooltip("When a UI element stands in for a world interactable, targeting is computed from it.")]
    [SerializeField] private InteractionProxySO _interactionProxy;

    private Vector2 _latestMousePosition;
    private bool _hasMouseMoved;
    private bool _wasClickPressedThisFrame;
    private Vector3Int? _lastHoveredCell;
    private Camera _mainCamera;

    private void OnEnable()
    {
        _inputReader.PointEvent += OnPointEvent;
        _inputReader.ClickStartedEvent += OnClickStarted;

        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(GridInputHandler)}: no {nameof(MainCameraAnchorSO)} assigned.", this);
            return;
        }

        _mainCamera = _mainCameraAnchor.Value;
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void OnDisable()
    {
        _inputReader.PointEvent -= OnPointEvent;
        _inputReader.ClickStartedEvent -= OnClickStarted;
        _wasClickPressedThisFrame = false;

        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera) => _mainCamera = camera;

    // This used to be an anonymous lambda that was never removed: InputReader is a ScriptableObject, so
    // every scene load added a dead closure to its invocation list, forever.
    private void OnClickStarted() => _wasClickPressedThisFrame = true;

    private void LateUpdate()
    {
        // Checked before the UI guard below, which would otherwise raise TargetingData.Empty and wipe the
        // ability preview the very frame the pointer enters the card that declared the proxy.
        if (TryHandleProxy()) return;

        if (IsPointerOverUI())
        {
            _wasClickPressedThisFrame = false;
            if (_lastHoveredCell.HasValue)
            {
                _lastHoveredCell = null;
                _onPointerMoved.RaiseEvent(TargetingData.Empty);
            }
            return;
        }

        TargetingData data = CalculateTargetingData();

        if (_hasMouseMoved || data.cellPosition != _lastHoveredCell)
        {
            _hasMouseMoved = false;
            _lastHoveredCell = data.cellPosition;
            _onPointerMoved.RaiseEvent(data);
        }

        if (_wasClickPressedThisFrame)
        {
            _onPointerClicked.RaiseEvent(data);
            _wasClickPressedThisFrame = false;
        }
    }

    /// <summary>
    /// Mirrors the pointer pipeline onto the element a UI card is standing in for.
    /// Returns true when the proxy is active and LateUpdate has nothing else to do this frame.
    /// </summary>
    private bool TryHandleProxy()
    {
        IInteractableElement proxied = _interactionProxy != null ? _interactionProxy.Current : null;
        if (proxied == null) return false;

        TargetingData data = BuildTargetingData(proxied.Transform.position, proxied as ITargettable);

        if (data.cellPosition != _lastHoveredCell)
        {
            _hasMouseMoved = false;
            _lastHoveredCell = data.cellPosition;
            _onPointerMoved.RaiseEvent(data);
        }

        if (_wasClickPressedThisFrame)
        {
            _onPointerClicked.RaiseEvent(data);
            _wasClickPressedThisFrame = false;
        }

        return true;
    }

    private TargetingData CalculateTargetingData()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_latestMousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 finalWorldPos = hit.point;
            if (hit.collider.TryGetComponent(out ITargettable target))
                finalWorldPos = hit.collider.transform.position;

            return BuildTargetingData(finalWorldPos, target);
        }
        return TargetingData.Empty;
    }

    private TargetingData BuildTargetingData(Vector3 worldPosition, ITargettable target)
    {
        Vector3Int cellPos;
        if (target is GridElement gridElement)
        {
            // The authoritative source: it is the very cell the element registers itself with in
            // GridStateDataSO (GridElement.InitializePosition), so targeting and occupancy cannot
            // diverge.
            cellPos = gridElement.gridPosition;
        }
        else
        {
            cellPos = _grid.WorldToCell(worldPosition);
            // The grid is single-layer (FloorMap only has tiles at z=0), but with a YZX cellSwizzle
            // and cellSize.z = 1 the cell's Z is the height above the deck: any point hit more than
            // 1 unit up would produce a Z matching no real cell at all.
            cellPos.z = 0;
        }

        bool isValid = _interactableTilemap.HasTile(cellPos);

        return new TargetingData(worldPosition, cellPos, isValid, target);
    }

    private void OnPointEvent(Vector2 screenPosition)
    {
        _latestMousePosition = screenPosition;
        _hasMouseMoved = true;
    }
    private bool IsPointerOverUI() => UIPointerTracker.IsPointerOverUI(_latestMousePosition);
}