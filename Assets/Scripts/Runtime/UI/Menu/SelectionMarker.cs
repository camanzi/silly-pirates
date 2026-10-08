using PrimeTween;
using UnityEngine.UIElements;

/// <summary>
/// The single shared "quill" that slides between menu rows. It is deliberately ONE element that
/// moves, not a marker owned by each row: unlike the underline (which needs two elements alive at
/// once, one erasing while another draws), only one entry is ever selected at a time, so a single
/// element with a position tween is enough and avoids visible popping between rows.
/// </summary>
[UxmlElement]
public partial class SelectionMarker : VisualElement
{
    private const string USS_CLASS_NAME = "selection-marker";
    private const string USS_CLASS_QUILL = "selection-marker__quill";

    // Two tweens rather than one 0..1 tween lerping both axes: a single tween would need the start
    // and end positions of both axes inside its callback, i.e. a capturing closure, while two
    // Tween.Custom calls on the element itself stay static and allocation-free.
    private Tween _slideTweenY;
    private Tween _slideTweenX;

    [UxmlAttribute] public float SlideDuration { get; set; } = 0.25f;
    [UxmlAttribute] public Ease SlideEase { get; set; } = Ease.OutCubic;

    public SelectionMarker()
    {
        AddToClassList(USS_CLASS_NAME);
        pickingMode = PickingMode.Ignore;

        var quill = new VisualElement { pickingMode = PickingMode.Ignore };
        quill.AddToClassList(USS_CLASS_QUILL);
        // Shared background-image / scale-mode / mirror; see .quill-icon in STL_Menu.uss.
        quill.AddToClassList("quill-icon");
        Add(quill);
    }

    /// <summary>
    /// Slides to <paramref name="target"/> (usually a <see cref="MenuButton"/>) on BOTH axes: Y to
    /// the target's row, X to the left edge of the target's TEXT (<see cref="MenuButton.TextLeft"/>),
    /// not of its box. Always, with no opt-out flag: entries are centred and have different widths
    /// (the pause menu's "Resume" vs "Return to main menu"), so a quill that only slid in Y would sit
    /// right next to one word and far from the next. The visible gap between quill and text is
    /// therefore NOT decided here: it is the negative <c>left</c> of <c>.selection-marker__quill</c>
    /// in each screen's USS, because that is a per-screen look, not behaviour.
    ///
    /// The marker must share its parent's coordinate space with the targets (sibling, or a sibling
    /// container placed at the same origin): <c>target.layout</c> is relative to the target's parent.
    /// Reads layout and text measurements, which are only meaningful after at least one resolved
    /// layout — call with <paramref name="animated"/> false for the very first placement.
    /// </summary>
    public void MoveTo(VisualElement target, bool animated)
    {
        float y = target.layout.y;
        float x = target.layout.x + (target is MenuButton button ? button.TextLeft : 0f);

        _slideTweenY.Stop();
        _slideTweenX.Stop();

        if (!animated || SlideDuration <= 0f)
        {
            style.top = y;
            style.left = x;
            return;
        }

        // Unscaled: the pause menu drives this at Time.timeScale 0.
        _slideTweenY = Tween.Custom(this, resolvedStyle.top, y, SlideDuration,
            static (element, value) => element.style.top = value, SlideEase, useUnscaledTime: true);
        _slideTweenX = Tween.Custom(this, resolvedStyle.left, x, SlideDuration,
            static (element, value) => element.style.left = value, SlideEase, useUnscaledTime: true);
    }

    public void Stop()
    {
        _slideTweenY.Stop();
        _slideTweenX.Stop();
    }
}
