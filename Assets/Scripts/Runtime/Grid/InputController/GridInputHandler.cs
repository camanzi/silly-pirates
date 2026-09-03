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
            Debug.LogError($"{nameof(GridInputHandler)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
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

    // Prima era una lambda anonima mai rimossa: InputReader è uno ScriptableObject, quindi ogni
    // caricamento scena aggiungeva una closure morta alla sua invocation list per sempre.
    private void OnClickStarted() => _wasClickPressedThisFrame = true;

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

            Vector3Int cellPos;
            if (target is GridElement gridElement)
            {
                // Fonte autorevole: è la stessa cella con cui l'elemento si registra in
                // GridStateDataSO (GridElement.InitializePosition), quindi targeting e
                // occupancy non possono divergere.
                cellPos = gridElement.gridPosition;
            }
            else
            {
                cellPos = _grid.WorldToCell(finalWorldPos);
                // La griglia è mono-layer (FloorMap ha tile solo su z=0), ma con
                // cellSwizzle YZX e cellSize.z = 1 la Z della cella è l'altezza sopra il
                // ponte: qualunque punto colpito sopra 1 unità produrrebbe una Z che non
                // corrisponde a nessuna cella reale.
                cellPos.z = 0;
            }

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