using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The loading overlay. Lives in the persistent scene, on a PanelSettings with a higher sorting
/// order than the HUD's: it must stay visible while the content scene underneath is unloaded and
/// reloaded.
///
/// Knows nothing about SceneFlowDirector: visibility arrives through a channel, so it holds no
/// reference to objects that can disappear with a scene.
///
/// The screen shows no progress, by design: the scenes here are small, and a percentage jumping
/// from 0 to 100 says less than nothing. In its place, two animations exist only so the screen is
/// not a frozen image — the dots and the quill's bob. The progress channel still exists and is
/// still raised by SceneFlowDirector, it simply has no one listening anymore.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    [Header("Channels")]
    [SerializeField] private BoolEventChannel _visibilityChannel;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.25f;

    private VisualElement _root;
    private EllipsisLabel _label;
    private LoadingAnimationElement _animation;

    private Tween _fadeTween;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;
        _root = documentRoot.Q<VisualElement>("loading-root");
        _label = documentRoot.Q<EllipsisLabel>("loading-label");
        _animation = documentRoot.Q<LoadingAnimationElement>("loading-animation");

        // Starts hidden: the first frame of the session must not show a full-screen overlay before
        // anyone has requested a transition.
        ApplyVisibility(false, instant: true);

        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised += HandleVisibility;
    }

    private void OnDisable()
    {
        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised -= HandleVisibility;

        _fadeTween.Stop();
        SetAnimationsRunning(false);
    }

    private void HandleVisibility(bool visible) => ApplyVisibility(visible, instant: false);

    private void ApplyVisibility(bool visible, bool instant)
    {
        if (_root == null) return;

        _fadeTween.Stop();
        SetAnimationsRunning(visible);

        if (instant)
        {
            _root.style.opacity = visible ? 1f : 0f;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return;
        }

        if (visible)
        {
            // Display must be turned on BEFORE the tween: an element in DisplayStyle.None is not
            // drawn at all, so the fade-in would not show.
            _root.style.display = DisplayStyle.Flex;
            _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
                static (el, v) => el.style.opacity = v, Ease.OutQuad);
            return;
        }

        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 0f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.InQuad)
            // Turning off the display comes after the fade, otherwise the overlay would stay
            // transparent but clickable, stealing input from the scene that just loaded.
            .OnComplete(_root, static el => el.style.display = DisplayStyle.None);
    }

    /// <summary>
    /// Dots and bob run only while the overlay is on screen: leaving them running would mean a
    /// scheduled tick and an infinite tween working for nothing for the rest of the match.
    /// </summary>
    private void SetAnimationsRunning(bool running)
    {
        if (running)
        {
            _label?.Play();
            _animation?.Play();
            return;
        }

        _label?.Stop();
        _animation?.Stop();
    }
}
