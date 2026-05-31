using UnityEngine;

public class WorldInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _maxDistance = 100f;

    [Header("Event Channels")]
    [SerializeField] private InteractableElementEventChannel _hoverChannel;

    private Camera _mainCamera;
    private IClickable _currentHovered;
    private Vector2 _mousePosition;

    private void Awake() => _mainCamera = Camera.main;

    private void OnEnable()
    {
        _inputReader.PointEvent += HandlePoint;
        _inputReader.ClickStartedEvent += HandleClick;
    }

    private void OnDisable()
    {
        _inputReader.PointEvent -= HandlePoint;
        _inputReader.ClickStartedEvent -= HandleClick;
    }

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
