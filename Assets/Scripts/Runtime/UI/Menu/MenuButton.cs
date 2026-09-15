using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A menu entry: a clickable label that draws its own underline and knows whether it currently
/// participates in keyboard/gamepad navigation.
///
/// Extends <see cref="Button"/> rather than <see cref="VisualElement"/> so the pointer/click
/// contract (hover, press, the <c>:disabled</c> pseudo-state) comes for free from
/// <see cref="Clickable"/> instead of being hand-rolled per screen — which was exactly the
/// duplication this refactor exists to remove. <c>focusable</c> is forced to false in the
/// constructor: focus and arrow-key navigation are owned by the screen root
/// (<c>NavigationMoveEvent</c> / <c>NavigationSubmitEvent</c>), not by the individual button. If a
/// button were ever made focusable, its own built-in submit handling would fire <see cref="clicked"/>
/// a second time on top of the root's handler.
///
/// The underline is a CHILD of the button and not a sibling element positioned in some shared
/// coordinate space: this is what lets <see cref="PlaceUnderline"/> work in the button's own local
/// content rect instead of reaching into a parent's layout, which is what the old stage-sized
/// "title-prompt" wrapper used to exist to work around.
/// </summary>
[UxmlElement]
public partial class MenuButton : Button
{
    private const string USS_CLASS_NAME = "menu-button";
    private const string USS_CLASS_DISABLED = "menu-button--disabled";
    private const string USS_CLASS_UNDERLINE = "menu-button__underline";

    private readonly VisualElement _underline;
    private Tween _underlineTween;
    private bool _selectable = true;

    /// <summary>Fired on pointer-enter, regardless of <see cref="Selectable"/>: the owning
    /// <see cref="MenuSelection"/> is the single place that decides what a hover is allowed to do.</summary>
    public event Action Hovered;

    /// <summary>Fired on a real click, and only then, never for a non-selectable entry.</summary>
    public event Action Activated;

    /// <summary>
    /// Whether this entry can be reached by arrow-key/hover navigation and can fire
    /// <see cref="Activated"/>. Toggles the disabled look itself so the USS class and this flag can
    /// never drift apart the way a hand-authored "selectable" bool plus a hand-authored CSS class on
    /// the same element could.
    /// </summary>
    [UxmlAttribute]
    public bool Selectable
    {
        get => _selectable;
        set
        {
            _selectable = value;
            EnableInClassList(USS_CLASS_DISABLED, !value);
        }
    }

    [UxmlAttribute] public float UnderlineInDuration { get; set; } = 0.24f;
    [UxmlAttribute] public Ease UnderlineInEase { get; set; } = Ease.OutQuad;
    [UxmlAttribute] public float UnderlineInDelay { get; set; } = 0.1f;
    [UxmlAttribute] public float UnderlineOutDuration { get; set; } = 0.16f;
    [UxmlAttribute] public Ease UnderlineOutEase { get; set; } = Ease.InQuad;

    public MenuButton()
    {
        AddToClassList(USS_CLASS_NAME);
        focusable = false;

        _underline = new VisualElement { pickingMode = PickingMode.Ignore };
        _underline.AddToClassList(USS_CLASS_UNDERLINE);
        Add(_underline);

        RegisterCallback<PointerEnterEvent>(_ => Hovered?.Invoke());
        clicked += () =>
        {
            if (_selectable) Activated?.Invoke();
        };
    }

    /// <summary>Programmatic equivalent of a pointer click, for keyboard/gamepad submit.</summary>
    public void ActivateFromKeyboard()
    {
        if (_selectable) Activated?.Invoke();
    }

    /// <summary>
    /// Draws or erases the underline. The two directions use different durations/eases on purpose:
    /// the abandoned entry's line erases from the right (shrinks in place) while the newly chosen
    /// one writes from the left, so at any instant one can be growing while another is shrinking.
    /// </summary>
    public void SetSelected(bool selected, bool animated)
    {
        _underlineTween.Stop();

        if (selected) DrawUnderline(animated);
        else EraseUnderline(animated);
    }

    /// <summary>
    /// Snaps the underline to its resting state without animating. Used on the first resolved
    /// layout (before that, text width measures as zero) and whenever the panel re-lays out.
    /// </summary>
    public void RefreshUnderline(bool selected)
    {
        _underlineTween.Stop();
        PlaceUnderline();
        _underline.style.width = selected ? MeasureTextWidth() : 0f;
    }

    public void StopUnderline() => _underlineTween.Stop();

    private void DrawUnderline(bool animated)
    {
        float target = MeasureTextWidth();
        PlaceUnderline();

        if (!animated || UnderlineInDuration <= 0f)
        {
            _underline.style.width = target;
            return;
        }

        _underlineTween = Tween.Custom(_underline, _underline.resolvedStyle.width, target,
            UnderlineInDuration, static (element, value) => element.style.width = value,
            UnderlineInEase, startDelay: Mathf.Max(0f, UnderlineInDelay), useUnscaledTime: true);
    }

    private void EraseUnderline(bool animated)
    {
        if (!animated || UnderlineOutDuration <= 0f)
        {
            _underline.style.width = 0f;
            return;
        }

        _underlineTween = Tween.Custom(_underline, _underline.resolvedStyle.width, 0f,
            UnderlineOutDuration, static (element, value) => element.style.width = value,
            UnderlineOutEase, useUnscaledTime: true);
    }

    /// <summary>
    /// Anchors the underline to the left edge of the TEXT, not of the button's box: the box is
    /// often much wider than the glyphs it centres (a 453px box for a much shorter word), so
    /// anchoring to the box edge would draw a line far longer than the word it sits under.
    /// </summary>
    private void PlaceUnderline()
    {
        float textWidth = MeasureTextWidth();
        float thickness = _underline.resolvedStyle.height > 0f ? _underline.resolvedStyle.height : 3f;

        _underline.style.left = (contentRect.width - textWidth) * 0.5f;
        _underline.style.top = contentRect.height - thickness;
    }

    /// <summary>
    /// Before the font is resolved, measuring returns zero/NaN; falling back to the content rect
    /// keeps the button usable (if visually approximate) until the real measurement is available,
    /// which the next <see cref="RefreshUnderline"/> call (first resolved layout) corrects.
    /// </summary>
    private float MeasureTextWidth()
    {
        float measured = MeasureTextSize(text, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).x;
        return float.IsNaN(measured) || measured <= 0f ? contentRect.width : measured;
    }
}
