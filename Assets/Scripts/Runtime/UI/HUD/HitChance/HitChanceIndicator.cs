using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class HitChanceIndicator : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private VisualElement _container;
    private Label _label;
    private Camera _camera;
    private ITargettable _hoveredTarget;
    private bool _isVisible;
    private Tween _visibilityTween;

    private void Awake()
    {
        _camera = Camera.main;
        var root = _uiDocument.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        _container = root.Q<VisualElement>("container");
        _label = root.Q<Label>("hit-chance-label");
        _container.style.display = DisplayStyle.None;
        _container.style.opacity = 0f;
    }

    public void HandlePayload(HighlightFreeAimPayload payload)
    {
        bool shouldShow = payload.HoveredTarget != null && payload.HitChance.HasValue;

        if (shouldShow)
        {
            _hoveredTarget = payload.HoveredTarget;
            _label.text = $"{Mathf.RoundToInt(payload.HitChance.Value)}%";
        }

        SetVisible(shouldShow);
    }

    private void SetVisible(bool show)
    {
        if (show == _isVisible) return;
        _isVisible = show;
        _visibilityTween.Stop();

        if (show)
        {
            _container.style.display = DisplayStyle.Flex;
            _visibilityTween = Tween.Custom(_container, _container.style.opacity.value, 1f, 0.15f,
                static (el, v) => el.style.opacity = v, Ease.OutQuad);
        }
        else
        {
            _hoveredTarget = null;
            _visibilityTween = Tween.Custom(_container, _container.style.opacity.value, 0f, 0.15f,
                static (el, v) => el.style.opacity = v, Ease.OutQuad)
                .OnComplete(_container, static el => el.style.display = DisplayStyle.None);
        }
    }

    private void LateUpdate()
    {
        if (!_isVisible || _hoveredTarget == null || _camera == null || _container.panel == null) return;

        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
            _container.panel, _hoveredTarget.Transform.position, _camera);
        _container.style.left = panelPos.x;
        _container.style.top = panelPos.y;
    }
}
