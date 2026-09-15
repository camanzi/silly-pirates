using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A small group of elements (typically a <see cref="SelectionMarker"/> and a <see cref="MenuButton"/>)
/// that fades in once and then pulses forever, as ONE opacity value. Exists because opacity in UI
/// Toolkit composites down the visual tree: animating the group's own opacity is enough to make
/// every child fade and pulse together, without each child running its own tween or knowing about
/// the others.
///
/// Starts <c>Visibility.Hidden</c> (not just transparent): layout must stay measurable, since a
/// <see cref="SelectionMarker"/> and a <see cref="MenuButton"/> both need resolved rectangles to
/// place themselves, but the group must not be clickable before it is actually shown.
/// </summary>
[UxmlElement]
public partial class PulsingGroup : VisualElement
{
    private const string USS_CLASS_NAME = "pulsing-group";

    [UxmlAttribute] public float FadeInDuration { get; set; } = 0.5f;
    [UxmlAttribute] public float PulseMinOpacity { get; set; } = 0.5f;
    [UxmlAttribute] public float PulseDuration { get; set; } = 1.25f;

    private Tween _fadeTween;
    private Tween _pulseTween;

    public PulsingGroup()
    {
        AddToClassList(USS_CLASS_NAME);
        pickingMode = PickingMode.Ignore;
        style.visibility = Visibility.Hidden;
        style.opacity = 0f;
    }

    public async Awaitable FadeInAsync(float startDelay)
    {
        style.visibility = Visibility.Visible;

        _fadeTween.Stop();
        _fadeTween = Tween.Custom(this, 0f, 1f, Mathf.Max(0.01f, FadeInDuration),
            static (element, value) => element.style.opacity = value, Ease.OutQuad,
            startDelay: Mathf.Max(0f, startDelay), useUnscaledTime: true);

        await _fadeTween;
    }

    /// <summary>Infinite pulse of the whole group. The configured value is the time of a single
    /// direction; a full Rewind cycle (there and back) takes twice as long.</summary>
    public void StartPulse()
    {
        _pulseTween.Stop();
        _pulseTween = Tween.Custom(this, 1f, Mathf.Clamp01(PulseMinOpacity), Mathf.Max(0.01f, PulseDuration),
            static (element, value) => element.style.opacity = value, Ease.InOutSine,
            cycles: -1, cycleMode: CycleMode.Rewind, useUnscaledTime: true);
    }

    public void StopPulse() => _pulseTween.Stop();

    public void Stop()
    {
        _fadeTween.Stop();
        _pulseTween.Stop();
    }
}
