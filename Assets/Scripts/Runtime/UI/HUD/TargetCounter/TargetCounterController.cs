using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class TargetCounterController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private TargetCounterEventChannel _targetCounterChannel;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Vector2 _cursorOffset = new(12f, 0f);

    private VisualElement _container;
    private Label _label;
    private Vector2 _screenPos;
    private bool _isVisible;
    private Tween _visibilityTween;

    private void Awake()
    {
        var root = _hudDocument.rootVisualElement;
        _container = root.Q<VisualElement>("target-counter-container");
        _label = root.Q<Label>("target-counter-label");
        _container.style.display = DisplayStyle.None;
        _container.style.opacity = 0f;
    }

    private void OnEnable()
    {
        _targetCounterChannel.OnEventRaised += HandlePayload;
        _inputReader.PointEvent += HandlePoint;
    }

    private void OnDisable()
    {
        _targetCounterChannel.OnEventRaised -= HandlePayload;
        _inputReader.PointEvent -= HandlePoint;
    }

    private void HandlePoint(Vector2 screenPos)
    {
        _screenPos = screenPos;
    }

    private void HandlePayload(TargetCounterPayload payload)
    {
        if (payload.IsActive)
        {
            _label.text = $"{payload.Current}/{payload.Max}";
            _label.EnableInClassList("target-counter-label--complete", payload.Current >= payload.Max);
        }

        SetVisible(payload.IsActive);

        if (payload.IsActive)
            UpdatePosition();
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
            _visibilityTween = Tween.Custom(_container, _container.style.opacity.value, 0f, 0.15f,
                static (el, v) => el.style.opacity = v, Ease.OutQuad)
                .OnComplete(_container, static el => el.style.display = DisplayStyle.None);
        }
    }

    private void LateUpdate()
    {
        if (!_isVisible || _container.panel == null) return;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_container.panel == null) return;

        // InputReader restituisce coordinate schermo con origine in basso a sinistra,
        // il panel space di UI Toolkit ha origine in alto a sinistra.
        Vector2 flippedScreenPos = new(_screenPos.x, Screen.height - _screenPos.y);

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_container.panel, flippedScreenPos);
        _container.style.left = panelPos.x + _cursorOffset.x;
        _container.style.top = panelPos.y + _cursorOffset.y;
    }
}
