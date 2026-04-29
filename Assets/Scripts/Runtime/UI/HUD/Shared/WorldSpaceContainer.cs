using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceContainer : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] protected UIDocument _uiDocument;
    protected VisualElement Container => _container;
    protected Camera MainCamera => _mainCamera; 
    private VisualElement _container;
    protected bool _isVisible = false;
    protected Tween _visibilityTween;
    protected bool _isRequested = false;
    protected bool _isAllowedByState = true;
    private Camera _mainCamera;
 
    protected virtual void OnEnable()
    {
        _mainCamera = Camera.main;
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");

        root.style.position = Position.Absolute;
        root.pickingMode = PickingMode.Ignore;

        if (_container != null) _container.pickingMode = PickingMode.Position;

        UpdateUIPosition();
    }

    public void SetStatePermission(bool isAllowed)
    {
        _isAllowedByState = isAllowed;
        ApplyVisibility();
    }

    public void ToggleRequested(bool show)
    {
        _isRequested = show;
        ApplyVisibility();
    }

    protected void ApplyVisibility()
    {
       bool finalVisibility = _isRequested && _isAllowedByState;

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
            _visibilityTween = Tween.Custom(0f, 1f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
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

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);
        
        if (screenPos.z > 0)
        {
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _container.panel, transform.position, _mainCamera);

            _container.style.left = panelPos.x;
            _container.style.top = panelPos.y;
        }
    }
}
