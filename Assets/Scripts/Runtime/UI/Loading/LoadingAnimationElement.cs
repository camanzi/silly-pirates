using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The loading screen's decorative animation. Named generically, and not e.g. "BobbingQuill",
/// because today's vertical bob is not guaranteed to be the final version — it could become a
/// frame-by-frame sequence (an animated sprite flip) later. <see cref="Play"/>/<see cref="Stop"/> is
/// the entire public contract <see cref="LoadingScreenController"/> depends on; a future switch to a
/// frame sequence would only touch the bob-specific attributes and the tween body below, never the
/// caller.
/// </summary>
[UxmlElement]
public partial class LoadingAnimationElement : VisualElement
{
    private const string USS_CLASS_NAME = "loading-animation";

    [UxmlAttribute] public float BobAmplitude { get; set; } = 6f;
    [UxmlAttribute] public float BobPeriod { get; set; } = 1.6f;
    [UxmlAttribute] public Ease BobEase { get; set; } = Ease.InOutSine;

    private Tween _bobTween;

    public LoadingAnimationElement()
    {
        AddToClassList(USS_CLASS_NAME);
        pickingMode = PickingMode.Ignore;
    }

    public void Play()
    {
        Stop();

        // The configured period is one FULL oscillation; a Rewind cycle covers half of it.
        _bobTween = Tween.Custom(this, -BobAmplitude, BobAmplitude, Mathf.Max(0.02f, BobPeriod) * 0.5f,
            static (element, value) => element.style.translate = new Translate(0f, value),
            BobEase, cycles: -1, cycleMode: CycleMode.Rewind, useUnscaledTime: true);
    }

    public void Stop() => _bobTween.Stop();
}
