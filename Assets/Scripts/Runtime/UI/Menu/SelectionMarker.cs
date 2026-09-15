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

    private Tween _slideTween;

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
    /// Slides to the vertical position of <paramref name="target"/> (usually a <see cref="MenuButton"/>).
    /// Reads <c>target.layout.y</c>, which is only meaningful after at least one resolved layout —
    /// call with <paramref name="animated"/> false for the very first placement.
    /// </summary>
    public void MoveTo(VisualElement target, bool animated)
    {
        float y = target.layout.y;
        _slideTween.Stop();

        if (!animated || SlideDuration <= 0f)
        {
            style.top = y;
            return;
        }

        _slideTween = Tween.Custom(this, resolvedStyle.top, y, SlideDuration,
            static (element, value) => element.style.top = value, SlideEase, useUnscaledTime: true);
    }

    public void Stop() => _slideTween.Stop();
}
