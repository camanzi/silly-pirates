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

    private Vector2 _latestMousePosition;
    private bool _hasMouseMoved;
    private bool _wasClickPressedThisFrame;
    private Vector3Int? _lastHoveredCell;
    private Camera _mainCamera;

    private void Awake() => _mainCamera = Camera.main;

    private void OnEnable()
    {
        _inputReader.PointEvent += OnPointEvent;
        _inputReader.ClickStartedEvent += () => _wasClickPressedThisFrame = true;
    }

    private void OnDisable()
    {
        _inputReader.PointEvent -= OnPointEvent;
        _wasClickPressedThisFrame = false;
    }

    private void LateUpdate()
    {
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

    private TargetingData CalculateTargetingData()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_latestMousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 finalWorldPos = hit.point;
            if (hit.collider.TryGetComponent(out ITargettable target))
                finalWorldPos = hit.collider.transform.position;

            Vector3Int cellPos = _grid.WorldToCell(finalWorldPos);
            bool isValid = _interactableTilemap.HasTile(cellPos);

            return new TargetingData(finalWorldPos, cellPos, isValid, target);
        }
        return TargetingData.Empty;
    }

    private void OnPointEvent(Vector2 screenPosition)
    {
        _latestMousePosition = screenPosition;
        _hasMouseMoved = true;
    }
    private bool IsPointerOverUI() => UIPointerTracker.IsPointerOverUI(_latestMousePosition);
}