using UnityEngine;

public class WorldInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _maxDistance = 100f;

    [Header("Event Channels")]
    [SerializeField] private InteractableElementEventChannel _hoverChannel;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    private Camera _mainCamera;
    private IClickable _currentHovered;
    private Vector2 _mousePosition;

    private void OnEnable()
    {
        _inputReader.PointEvent += HandlePoint;
        _inputReader.ClickStartedEvent += HandleClick;

        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(WorldInteractor)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
            return;
        }

        _mainCamera = _mainCameraAnchor.Value;
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void OnDisable()
    {
        _inputReader.PointEvent -= HandlePoint;
        _inputReader.ClickStartedEvent -= HandleClick;

        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera) => _mainCamera = camera;

    private void HandlePoint(Vector2 pos) => _mousePosition = pos;

    private void HandleClick()
    {
        if (UIPointerTracker.IsPointerOverUI(_mousePosition)) return;

        _currentHovered?.OnClick();
    }

    private void Update()
    {
        PerformHoverCheck();
    }

    private void PerformHoverCheck()
    {
        // Con l'auto-bootstrap la camera può arrivare un frame dopo questo componente:
        // senza guardia, ScreenPointToRay su null esplode ogni frame finché non arriva.
        if (_mainCamera == null) return;

        if (UIPointerTracker.IsPointerOverUI(_mousePosition))
        {
            ResetHover();
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(_mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _interactableLayer))
        {
            IClickable interactable = hit.collider.GetComponent<IClickable>();

            if (interactable != _currentHovered)
            {
                ResetHover();
                _currentHovered = interactable;
                _currentHovered?.OnHoverEnter();
                _hoverChannel?.RaiseEvent(_currentHovered as IInteractableElement);
            }
        }
        else
        {
            ResetHover();
        }
    }

    private void ResetHover()
    {
        if (_currentHovered != null)
        {
            _currentHovered.OnHoverExit();
            _hoverChannel?.RaiseEvent(null);
            _currentHovered = null;
        }
    }
}
