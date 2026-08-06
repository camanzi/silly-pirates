using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Nasconde la HUD durante la sequenza di intro al combattimento e la fa comparire con un
/// fade-in quando <see cref="ShowHud"/> viene invocato (tipicamente da un listener su
/// <c>OnCombatStartedEventChannel</c>). Nelle scene senza sequencer di intro
/// (<see cref="CombatIntroStateSO.IsIntroActive"/> sempre falso) la HUD resta visibile fin
/// dal primo frame, senza alcuna regressione.
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
