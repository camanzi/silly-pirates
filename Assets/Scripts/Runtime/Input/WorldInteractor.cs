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

    [Header("Interaction")]
    [Tooltip("When a UI element stands in for a world interactable, hover and click are routed to it.")]
    [SerializeField] private InteractionProxySO _interactionProxy;

    private Camera _mainCamera;
    private IClickable _currentHovered;
    private Vector2 _mousePosition;

    private void OnEnable()
    {
        _inputReader.PointEvent += HandlePoint;
        _inputReader.ClickStartedEvent += HandleClick;

        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(WorldInteractor)}: no {nameof(MainCameraAnchorSO)} assigned.", this);
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
        // A click on UI is swallowed, unless that UI is currently proxying a world element: in that case the
        // click belongs to the proxied element, exactly as if it had been clicked in the world.
        if (UIPointerTracker.IsPointerOverUI(_mousePosition) && _interactionProxy?.Current == null) return;

        _currentHovered?.OnClick();
    }

    private void Update()
    {
        PerformHoverCheck();
    }

    private void PerformHoverCheck()
    {
        // With auto-bootstrap the camera can arrive a frame after this component: without the guard,
        // ScreenPointToRay on null blows up every frame until it does.
        if (_mainCamera == null) return;

        // A proxied element wins over both the UI guard and the raycast: the pointer is over the UI by
        // definition, and the element it stands for may well be off screen.
        IClickable proxied = _interactionProxy != null ? _interactionProxy.Current : null;
        if (proxied != null)
        {
            if (proxied != _currentHovered)
            {
                ResetHover();
                _currentHovered = proxied;
                _currentHovered.OnHoverEnter();
                _hoverChannel?.RaiseEvent(_currentHovered as IInteractableElement);
            }
            return;
        }

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
