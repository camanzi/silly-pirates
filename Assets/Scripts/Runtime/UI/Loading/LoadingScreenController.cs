using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Overlay di caricamento. Vive nella scena persistente, su un PanelSettings con sortingOrder più
/// alto di quello della HUD: deve restare visibile mentre la scena di contenuto sotto viene
/// scaricata e ricaricata.
///
/// Non conosce SceneFlowDirector: riceve avanzamento e visibilità da due canali, quindi non ha
/// riferimenti a oggetti che possono sparire con una scena.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    [Header("Canali")]
    [SerializeField] private FloatEventChannel _progressChannel;
    [SerializeField] private BoolEventChannel _visibilityChannel;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.25f;

    private VisualElement _root;
    private VisualElement _progressFill;
    private Label _percentageLabel;
    private Tween _fadeTween;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;
        _root = documentRoot.Q<VisualElement>("loading-root");
        _progressFill = documentRoot.Q<VisualElement>("progress-fill");
        _percentageLabel = documentRoot.Q<Label>("loading-percentage");

        // Parte nascosto: il primo frame della sessione non deve mostrare un overlay a schermo pieno
        // prima che qualcuno abbia chiesto una transizione.
        ApplyVisibility(false, instant: true);

        if (_progressChannel != null) _progressChannel.OnEventRaised += HandleProgress;
        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised += HandleVisibility;
    }

    private void OnDisable()
    {
        if (_progressChannel != null) _progressChannel.OnEventRaised -= HandleProgress;
        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised -= HandleVisibility;

        _fadeTween.Stop();
    }

    private void HandleProgress(float normalized)
    {
        float clamped = Mathf.Clamp01(normalized);

        if (_progressFill != null) _progressFill.style.width = Length.Percent(clamped * 100f);
        if (_percentageLabel != null) _percentageLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }

    private void HandleVisibility(bool visible) => ApplyVisibility(visible, instant: false);

    private void ApplyVisibility(bool visible, bool instant)
    {
        if (_root == null) return;

        _fadeTween.Stop();

        if (instant)
        {
            _root.style.opacity = visible ? 1f : 0f;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return;
        }

        if (visible)
        {
            // Il display va acceso PRIMA del tween: un elemento in DisplayStyle.None non viene
            // disegnato, quindi il fade-in non si vedrebbe affatto.
            _root.style.display = DisplayStyle.Flex;
            _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
                static (el, v) => el.style.opacity = v, Ease.OutQuad);
            return;
        }

        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 0f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.InQuad)
            // Lo spegnimento del display va in coda al fade, altrimenti l'overlay resterebbe
            // trasparente ma cliccabile, rubando gli input alla scena appena caricata.
            .OnComplete(_root, static el => el.style.display = DisplayStyle.None);
    }
}
