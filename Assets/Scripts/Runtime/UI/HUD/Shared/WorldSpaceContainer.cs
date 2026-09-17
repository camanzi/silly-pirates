using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceContainer : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] protected UIDocument _uiDocument;
    [Tooltip("If true try to show the menu by default without requesting it by proximity")]
    [SerializeField] private bool _shownByDefault;
    [SerializeField] protected Vector3 _positionOffset;
    [SerializeField] protected PickingMode _pickingMode = PickingMode.Ignore;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    protected VisualElement Container => _container;
    protected Camera MainCamera => _mainCamera; 
    private VisualElement _container;
    protected bool _isVisible = false;
    protected Tween _visibilityTween;
    protected bool _isRequested = false;
    protected bool _isAllowedByCombatState = true;
    protected bool _isAllowedByElementState = true;
    private Camera _mainCamera;
 
    protected virtual void OnEnable()
    {
        if (_mainCameraAnchor != null)
        {
            _mainCamera = _mainCameraAnchor.Value;
            _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
        }
        else
        {
            Debug.LogError($"{GetType().Name}: no {nameof(MainCameraAnchorSO)} assigned.", this);
        }

        UpdateUIPosition();
        UIPointerTracker.Register(_uiDocument);
    }

    protected virtual void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
        UIPointerTracker.Unregister(_uiDocument);
    }

    private void HandleCameraChanged(Camera camera) => _mainCamera = camera;

    protected virtual void Awake()
    {
        _isRequested = _shownByDefault;
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");

        root.style.position = Position.Absolute;
        root.pickingMode = PickingMode.Ignore;

        if (_container != null)
        {
            _container.pickingMode = _pickingMode;
            _container.RegisterCallback<GeometryChangedEvent>(evt => {
                if (evt.oldRect.size != evt.newRect.size)
                    UpdateUIPosition();
            });
        }

        // Aligns _isVisible/opacity/display to the current gate right away, with no tween: on frame zero
        // _isVisible starts false by field default, so a normal ApplyVisibility() would attempt a fade-out
        // from opacity 1 (a flash) instead of simply hiding quietly.
        ApplyVisibilityImmediate();
    }

    // Use when combat state changes and the element should hide his UI
    public void SetCombatStatePermission(bool isAllowed)
    {
        _isAllowedByCombatState = isAllowed;
        ApplyVisibility();
    }

    // Use when element should hide his UI for some internal states
    public void SetElementStatePermission(bool isAllowed)
    {
        _isAllowedByElementState = isAllowed;
        ApplyVisibility();
    }
    
    public void ToggleRequested(bool show)
    {
        _isRequested = show;
        ApplyVisibility();
    }


    // AND gate of the three permissions: explicit request (proximity/menu), combat state (e.g. the
    // intro), the element's own internal state (e.g. death). Shared between ApplyVisibility() and
    // ApplyVisibilityImmediate() so the two formulas cannot drift apart over time.
    private bool FinalVisibility => _isRequested && _isAllowedByCombatState && _isAllowedByElementState;

    protected void ApplyVisibility()
    {
        if (Container == null) return;

        bool finalVisibility = FinalVisibility;

        if (finalVisibility == _isVisible)
        {
            if (finalVisibility)
                RefreshUI();
            return;
        }

        _visibilityTween.Stop();
        _isVisible = finalVisibility;

        if (finalVisibility)
        {
            // The container may have been set to display:None by an earlier OnCompleteHide: it has to be
            // restored before animating the opacity, or the tween writes to an element the layout does
            // not render anyway.
            Container.style.display = DisplayStyle.Flex;
            ShowUI();
            float startOpacity = Container.style.opacity.value;
            _visibilityTween = Tween.Custom(startOpacity, 1f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => OnCompleteShow());
        }
        else
        {
            _visibilityTween = Tween.Custom(Container.style.opacity.value, 0f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => OnCompleteHide());
        }
    }

    /// <summary>
    /// Applies the current gate with no tween, writing opacity and display within a single frame.
    /// Use it in Awake (the first usable frame, where a fade from opacity 1 would flash) and every time a
    /// permission is decided before the container has ever been shown once.
    /// </summary>
    protected void ApplyVisibilityImmediate()
    {
        if (Container == null) return;

        _visibilityTween.Stop();
        bool finalVisibility = FinalVisibility;
        _isVisible = finalVisibility;

        Container.style.opacity = finalVisibility ? 1f : 0f;
        Container.style.display = finalVisibility ? DisplayStyle.Flex : DisplayStyle.None;
    }

    protected virtual void ShowUI() {}

    protected virtual void RefreshUI() {}

    protected virtual void OnCompleteHide()
    {
        if (!_isVisible)
            Container.style.display = DisplayStyle.None;
    }

    protected virtual void OnCompleteShow()
    {
        
    }

    protected virtual void LateUpdate()
    {
        UpdateUIPosition();
    }

    protected void UpdateUIPosition()
    {
        if (_mainCamera == null || _container == null || _container.style.display == DisplayStyle.None) 
            return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position + _positionOffset);

        if (screenPos.z > 0)
        {
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _container.panel, transform.position + _positionOffset, _mainCamera);

            _container.style.left = panelPos.x;
            _container.style.top = panelPos.y;
        }
    }
}
