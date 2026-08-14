using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageNumberManager : MonoBehaviour
{
    [SerializeField] private DamageEventChannel _damageEventChannel;
    [SerializeField] private VisualTreeAsset _damageNumberTemplate;
    [SerializeField] private VisualTreeAsset _modifierLabelTemplate;
    [SerializeField] private DamageTypeColorConfigSO _colorConfig;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    [Header("Animation Settings")]
    [SerializeField] private float _normalFontSize = 80f;
    [SerializeField] private float _critFontSize = 100f;
    [SerializeField] private float _modifierFontSize = 60f;
    [SerializeField] private float _shrinkAmount = 20f;
    [SerializeField] private float _growDuration = 0.2f;
    [SerializeField] private float _shrinkDuration = 1f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private float _critFadeInDuration = 0.5f;
    [SerializeField] private float _critFadeInDelay = 0.5f;
    [SerializeField] [Range(0.2f, 1f)] private float _critLabelSizeRatio = 0.5f;
    [SerializeField] private float _spawnRadius = 1.5f;

    private readonly List<(VisualElement popup, Vector3 worldPos)> _activePopups = new();
    private VisualElement _root;
    private Camera _camera;

    private void OnEnable()
    {
        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(DamageNumberManager)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
            return;
        }

        _camera = _mainCameraAnchor.Value;
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera) => _camera = camera;

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.pickingMode = PickingMode.Ignore;
    }

    private void LateUpdate()
    {
        if (_root == null || _camera == null) return;
        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            var (popup, worldPos) = _activePopups[i];
            if (popup.parent == null)
            {
                _activePopups.RemoveAt(i);
                continue;
            }
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, worldPos, _camera);
            popup.style.left = panelPos.x;
            popup.style.top = panelPos.y;
        }
    }

    private Vector3 ScatteredPosition(Vector3 center)
    {
        Vector2 r = Random.insideUnitCircle * _spawnRadius;
        return center + new Vector3(r.x, 0f, r.y);
    }

    public void SpawnDamageNumber(DamageEvent evt)
    {
        if (_root == null) return;

        if (evt.Payload.IsMiss)
        {
            SpawnModifierLabel("Miss", new Color(0.8f, 0.8f, 0.8f), evt.WorldPosition);
            return;
        }

        Color elementColor = _colorConfig != null ? _colorConfig.GetColor(evt.Payload.Type) : Color.white;

        var popup = _damageNumberTemplate.CloneTree();
        popup.pickingMode = PickingMode.Ignore;

        bool isCrit = evt.Payload.IsCritical;
        float startFontSize = isCrit ? _critFontSize : _normalFontSize;

        var damageLabel = popup.Q<Label>("damage-label");
        var critLabel = popup.Q<Label>("crit-label");

        damageLabel.text = Mathf.RoundToInt(evt.Payload.Amount).ToString();
        damageLabel.style.color = new StyleColor(elementColor);
        critLabel.style.color = new StyleColor(elementColor);
        popup.style.fontSize = 0;
        critLabel.style.fontSize = startFontSize * _critLabelSizeRatio;

        _root.Add(popup);
        _activePopups.Add((popup, ScatteredPosition(evt.WorldPosition)));

        Sequence.Create()
            .Chain(Tween.Custom(popup, 0f, startFontSize, _growDuration,
                static (el, v) => el.style.fontSize = v, Ease.OutBack))
            .Chain(Tween.Custom(popup, startFontSize, startFontSize - _shrinkAmount, _shrinkDuration,
                static (el, v) => el.style.fontSize = v, Ease.Linear))
            .Chain(Tween.Custom(popup, 1f, 0f, _fadeOutDuration,
                static (el, v) => el.style.opacity = v, Ease.Linear))
            .OnComplete(popup, static el => el.RemoveFromHierarchy());

        if (isCrit)
        {
            Tween.Custom(critLabel, 0f, 1f, _critFadeInDuration,
                static (el, v) => el.style.opacity = v, Ease.Linear,
                startDelay: _critFadeInDelay);
        }

        const float epsilon = 0.01f;
        float ratio = evt.Payload.ResistanceMultiplier;
        if (Mathf.Abs(ratio - 1f) > epsilon)
            SpawnModifierLabel(ratio < 1f ? "Resist" : "Effective", elementColor, evt.WorldPosition);
    }

    private void SpawnModifierLabel(string text, Color color, Vector3 center)
    {
        if (_modifierLabelTemplate == null) return;

        var popup = _modifierLabelTemplate.CloneTree();
        popup.pickingMode = PickingMode.Ignore;

        var label = popup.Q<Label>("modifier-label");
        label.text = text;
        label.style.color = new StyleColor(color);
        popup.style.fontSize = 0;

        _root.Add(popup);
        _activePopups.Add((popup, ScatteredPosition(center)));

        Sequence.Create()
            .Chain(Tween.Custom(popup, 0f, _modifierFontSize, _growDuration,
                static (el, v) => el.style.fontSize = v, Ease.OutBack))
            .Chain(Tween.Custom(popup, _modifierFontSize, _modifierFontSize - _shrinkAmount, _shrinkDuration,
                static (el, v) => el.style.fontSize = v, Ease.Linear))
            .Chain(Tween.Custom(popup, 1f, 0f, _fadeOutDuration,
                static (el, v) => el.style.opacity = v, Ease.Linear))
            .OnComplete(popup, static el => el.RemoveFromHierarchy());
    }
}
