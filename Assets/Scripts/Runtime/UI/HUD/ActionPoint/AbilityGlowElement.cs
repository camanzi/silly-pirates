using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Radial glow drawn with Painter2D as a stack of concentric rings with a decreasing
/// alpha falloff. The mesh is generated once (and only regenerated on layout changes),
/// so the pulse animation only touches style.scale / style.opacity and costs no repaint.
/// </summary>
[UxmlElement]
public partial class AbilityGlowElement : VisualElement
{
    private int _ringCount = 24;
    private float _innerRadiusRatio = 0.28f;
    private float _outerRadiusRatio = 0.5f;
    private float _falloff = 2.2f;
    private float _peakAlpha = 0.85f;
    private Color _glowColor = new(1f, 0.82f, 0.35f);

    private Tween _opacityTween;
    private Tween _scaleTween;

    [UxmlAttribute]
    public int RingCount
    {
        get => _ringCount;
        set { _ringCount = Mathf.Max(1, value); MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float InnerRadiusRatio
    {
        get => _innerRadiusRatio;
        set { _innerRadiusRatio = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float OuterRadiusRatio
    {
        get => _outerRadiusRatio;
        set { _outerRadiusRatio = value; MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float Falloff
    {
        get => _falloff;
        set { _falloff = Mathf.Max(0.01f, value); MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public float PeakAlpha
    {
        get => _peakAlpha;
        set { _peakAlpha = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }

    [UxmlAttribute]
    public Color GlowColor
    {
        get => _glowColor;
        set { _glowColor = value; MarkDirtyRepaint(); }
    }

    public AbilityGlowElement()
    {
        AddToClassList("ability-glow");
        pickingMode = PickingMode.Ignore;

        generateVisualContent += OnGenerateGlow;
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt) => MarkDirtyRepaint();

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Authoring panels (UI Builder) keep the glow visible so its look can be tweaked;
        // runtime panels start hidden and are driven by ActiveAbilityButtonController.
        if (evt.destinationPanel.contextType == ContextType.Player)
            style.opacity = 0f;
    }

    private void OnGenerateGlow(MeshGenerationContext ctx)
    {
        float side = Mathf.Min(contentRect.width, contentRect.height);
        if (side <= 0f) return;

        var painter = ctx.painter2D;
        var center = new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f);
        float rIn = side * _innerRadiusRatio;
        float rOut = side * _outerRadiusRatio;

        // Solid core, so the centre of the "sun" is not hollow.
        painter.fillColor = new Color(_glowColor.r, _glowColor.g, _glowColor.b, _peakAlpha);
        painter.BeginPath();
        painter.Arc(center, rIn, 0f, 360f);
        painter.Fill();

        // Overlap the rings (x2.2) so no banding is visible between steps.
        painter.lineWidth = (rOut - rIn) / _ringCount * 2.2f;

        for (int i = 0; i < _ringCount; i++)
        {
            float t = _ringCount > 1 ? (float)i / (_ringCount - 1) : 0f;
            float r = Mathf.Lerp(rIn, rOut, t);
            float a = Mathf.Pow(1f - t, _falloff) * _peakAlpha;

            painter.strokeColor = new Color(_glowColor.r, _glowColor.g, _glowColor.b, a);
            painter.BeginPath();
            painter.Arc(center, r, 0f, 360f);
            painter.Stroke();
        }
    }

    public void PlayIn(float duration = 0.2f)
    {
        _opacityTween.Stop();
        _scaleTween.Stop();

        SetScale(1f);
        _opacityTween = Tween.Custom(this, resolvedStyle.opacity, 1f, duration, ease: Ease.OutQuad,
            onValueChange: (target, val) => target.style.opacity = val)
            .OnComplete(this, static target => target.StartPulse());
    }

    public void StartPulse()
    {
        _opacityTween.Stop();
        _scaleTween.Stop();

        _scaleTween = Tween.Custom(this, 1f, 1.12f, duration: 1.1f,
            cycles: -1, cycleMode: CycleMode.Rewind,
            onValueChange: (target, val) => target.SetScale(val));

        _opacityTween = Tween.Custom(this, 1f, 0.75f, duration: 1.1f,
            cycles: -1, cycleMode: CycleMode.Rewind,
            onValueChange: (target, val) => target.style.opacity = val);
    }

    public void PlayOut(float duration = 0.15f)
    {
        _opacityTween.Stop();
        _scaleTween.Stop();

        SetScale(1f);
        _opacityTween = Tween.Custom(this, resolvedStyle.opacity, 0f, duration, ease: Ease.OutQuad,
            onValueChange: (target, val) => target.style.opacity = val);
    }

    /// <summary>One-shot burst used to confirm an instant (non-targeted) ability cast.</summary>
    public void Flash(float duration = 0.35f)
    {
        _opacityTween.Stop();
        _scaleTween.Stop();

        float half = duration * 0.5f;

        _opacityTween = Tween.Custom(this, 0f, 1f, half, ease: Ease.OutQuad,
            cycles: 2, cycleMode: CycleMode.Rewind,
            onValueChange: (target, val) => target.style.opacity = val);

        _scaleTween = Tween.Custom(this, 0.9f, 1.35f, half, ease: Ease.OutQuad,
            cycles: 2, cycleMode: CycleMode.Rewind,
            onValueChange: (target, val) => target.SetScale(val));
    }

    public void StopAll()
    {
        _opacityTween.Stop();
        _scaleTween.Stop();
        style.opacity = 0f;
        SetScale(1f);
    }

    private void SetScale(float uniform) =>
        style.scale = new StyleScale(new Scale(new Vector3(uniform, uniform, 1f)));
}
