using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Hides the HUD during the combat intro sequence and brings it in with a fade when
/// <see cref="ShowHud"/> is invoked (typically by a listener on <c>OnCombatStartedEventChannel</c>).
/// In scenes with no intro sequencer (<see cref="CombatIntroStateSO.IsIntroActive"/> always false) the
/// HUD stays visible from the very first frame, with no regression.
/// </summary>
public class HudVisibilityController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private CombatIntroStateSO _introState;
    [SerializeField] private float _fadeDuration = 0.4f;

    private VisualElement _root;
    private bool _isVisible = true;
    private Tween _visibilityTween;

    private void OnEnable()
    {
        _root = _hudDocument.rootVisualElement;

        if (_introState != null && _introState.IsIntroActive)
        {
            _isVisible = false;
            _root.style.opacity = 0f;
            _root.style.display = DisplayStyle.None;
        }
    }

    public void ShowHud()
    {
        if (_isVisible) return;
        _isVisible = true;
        _visibilityTween.Stop();

        _root.style.display = DisplayStyle.Flex;
        _visibilityTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.OutQuad);
    }
}
