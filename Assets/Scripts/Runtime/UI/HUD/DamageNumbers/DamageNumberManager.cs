using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageNumberManager : MonoBehaviour
{
    [SerializeField] private DamageEventChannel _damageEventChannel;
    [SerializeField] private VisualTreeAsset _damageNumberTemplate;

    [Header("Animation Settings")]
    [SerializeField] private float _normalFontSize = 80f;
    [SerializeField] private float _critFontSize = 100f;
    [SerializeField] private float _shrinkAmount = 20f;
    [SerializeField] private float _growDuration = 0.2f;
    [SerializeField] private float _shrinkDuration = 1f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private float _critFadeInDuration = 0.5f;
    [SerializeField] private float _critFadeInDelay = 0.5f;
    [SerializeField] [Range(0.2f, 1f)] private float _critLabelSizeRatio = 0.5f;

    private readonly List<(VisualElement popup, Vector3 worldPos)> _activePopups = new();
    private VisualElement _root;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

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

    public void SpawnDamageNumber(DamageEvent evt)
    {
        if (_root == null) return;

        var popup = _damageNumberTemplate.CloneTree();
        popup.pickingMode = PickingMode.Ignore;

        bool isCrit = evt.Payload.IsCritical;
        float startFontSize = isCrit ? _critFontSize : _normalFontSize;

        var damageLabel = popup.Q<Label>("damage-label");
        var critLabel = popup.Q<Label>("crit-label");

        damageLabel.text = Mathf.RoundToInt(evt.Payload.Amount).ToString();
        popup.style.fontSize = 0;
        critLabel.style.fontSize = startFontSize * _critLabelSizeRatio;

        _root.Add(popup);
        _activePopups.Add((popup, evt.WorldPosition));

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
    }


}
