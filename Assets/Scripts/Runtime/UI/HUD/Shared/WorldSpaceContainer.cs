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
        _mainCamera = Camera.main;
        UpdateUIPosition();
        UIPointerTracker.Register(_uiDocument);
    }

    protected virtual void OnDisable()
    {
        UIPointerTracker.Unregister(_uiDocument);
    }

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


    protected void ApplyVisibility()
    {
       bool finalVisibility = _isRequested && _isAllowedByCombatState && _isAllowedByElementState;

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
