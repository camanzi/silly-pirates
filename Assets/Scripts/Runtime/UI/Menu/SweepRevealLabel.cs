using System.Text;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A label whose text is revealed by a soft-edged mask sweeping left to right, glyph by glyph,
/// instead of appearing all at once or being typed out character by character.
///
/// The mask is a function of horizontal POSITION, not of character index: with a wide falloff,
/// several glyphs are always mid-fade together and the sweep reads as continuous motion rather than
/// a ticking counter. A real overlay mask was not an option: hiding the text would require painting
/// over it in the parchment's own colour, but the parchment is a texture, and a flat-colour patch
/// over it would be visibly wrong.
/// </summary>
/// <remarks>
/// The per-glyph alpha is emitted as <c>&lt;color=#RRGGBBAA&gt;</c> and never as
/// <c>&lt;alpha=#AA&gt;</c>. UI Toolkit's rich text parser RECOGNISES <c>&lt;alpha&gt;</c> as a tag
/// (it disappears from the rendered text) but does not apply it — the text stays fully opaque with
/// no warning anywhere. This was verified on screen, not assumed from documentation; do not "clean
/// this up" to the shorter tag, it will silently break the reveal.
/// </remarks>
[UxmlElement]
public partial class SweepRevealLabel : Label
{
    private const string USS_CLASS_NAME = "sweep-reveal-label";
    private const string HEX_DIGITS = "0123456789ABCDEF";

    [UxmlAttribute] public float SweepDuration { get; set; } = 2f;
    [UxmlAttribute] public float SweepFalloff { get; set; } = 300f;
    [UxmlAttribute] public Ease SweepEase { get; set; } = Ease.InOutSine;

    // Captured lazily and not in the constructor: the UXML `text` attribute is applied by the
    // UxmlSerializedData machinery AFTER the element is constructed, so `text` would still be empty
    // here. Capturing on first use also means the UI Builder canvas keeps showing the authored text
    // instead of being permanently blanked out by an eagerly-applied hidden state.
    private string _text;
    private readonly StringBuilder _builder = new();

    // Horizontal centre of every glyph, in the label's own text-measurement space. The mask reads
    // these, not the character index, so its alpha is a function of position.
    private float[] _glyphCentres;
    private float _textWidth;

    // The label's own colour in RRGGBB, read from the resolved style rather than hardcoded: the
    // per-glyph alpha travels inside a <color=#RRGGBBAA> tag, which re-declares the colour too, so
    // hardcoding it here would mean restyling the label silently stops having any visible effect.
    private string _colourHex = "000000";

    private Tween _sweep;

    public SweepRevealLabel() => AddToClassList(USS_CLASS_NAME);

    /// <summary>
    /// Writes the fully-hidden starting state immediately. Call this right after the element is
    /// wired up, before the first frame is drawn — waiting until the first layout would show the
    /// fully opaque text for a frame before it disappears.
    /// </summary>
    public void Hide()
    {
        CaptureTextIfNeeded();
        text = "<color=#00000000>" + _text;
    }

    /// <summary>
    /// Runs the sweep. Must be called at resolved layout: before that, text measurement returns
    /// zero and every glyph would collapse to the same point. Restores the plain, tag-free text on
    /// completion so a label that no longer needs the markup does not keep carrying it forever.
    /// </summary>
    public async Awaitable RevealAsync(CancellationToken token)
    {
        CaptureTextIfNeeded();
        MeasureGlyphs();
        ResolveColour();

        _sweep.Stop();
        _sweep = Tween.Custom(this, -SweepFalloff, _textWidth + SweepFalloff,
            Mathf.Max(0.01f, SweepDuration), static (label, edge) => label.ApplySweep(edge),
            SweepEase, useUnscaledTime: true);

        await _sweep;
        if (token.IsCancellationRequested) return;

        text = _text;
    }

    public void Stop() => _sweep.Stop();

    private void CaptureTextIfNeeded() => _text ??= text ?? string.Empty;

    /// <summary>
    /// Cumulative widths of the text's prefixes: each glyph's position is the one it actually has
    /// inside the whole string, kerning included. Splitting the title into one label per character
    /// would give the wrong spacing instead, visible with a calligraphic font like Italianno.
    /// </summary>
    private void MeasureGlyphs()
    {
        _glyphCentres = new float[_text.Length];

        float previous = 0f;
        bool measurable = _text.Length > 0;

        for (int i = 0; i < _text.Length; i++)
        {
            float upTo = Measure(_text.Substring(0, i + 1));

            if (upTo <= 0f)
            {
                measurable = false;
                break;
            }

            _glyphCentres[i] = (previous + upTo) * 0.5f;
            previous = upTo;
        }

        if (measurable && previous > 0f)
        {
            _textWidth = previous;
            return;
        }

        // Font not resolved yet: fall back to an even spacing across the label's own content rect.
        // The effect stays a sweeping mask, just with glyphs distributed by eye instead of measured.
        _textWidth = contentRect.width;
        float step = _text.Length > 0 ? _textWidth / _text.Length : 0f;

        for (int i = 0; i < _text.Length; i++)
            _glyphCentres[i] = (i + 0.5f) * step;
    }

    private float Measure(string value)
    {
        float measured = MeasureTextSize(value, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).x;
        return float.IsNaN(measured) ? 0f : measured;
    }

    /// <summary>
    /// Rebuilds the string with one alpha tag per glyph. Runs every frame for the duration of the
    /// sweep: the StringBuilder is reused and a tag is only emitted when the value CHANGES, so the
    /// fully-opaque and fully-invisible regions cost nothing extra.
    /// </summary>
    private void ApplySweep(float edgeX)
    {
        float falloff = Mathf.Max(1f, SweepFalloff);

        _builder.Clear();
        int lastAlpha = -1;

        for (int i = 0; i < _text.Length; i++)
        {
            float alpha = Mathf.Clamp01((edgeX - _glyphCentres[i]) / falloff);
            int quantised = Mathf.RoundToInt(alpha * 255f);

            if (quantised != lastAlpha)
            {
                _builder.Append("<color=#");
                _builder.Append(_colourHex);
                AppendHex(_builder, quantised);
                _builder.Append('>');
                lastAlpha = quantised;
            }

            _builder.Append(_text[i]);
        }

        text = _builder.ToString();
    }

    /// <summary>Must run at resolved style: before the first layout, resolvedStyle.color is not the
    /// sheet's real value yet, and the title would flash the wrong colour.</summary>
    private void ResolveColour()
    {
        Color32 colour = resolvedStyle.color;

        _builder.Clear();
        AppendHex(_builder, colour.r);
        AppendHex(_builder, colour.g);
        AppendHex(_builder, colour.b);
        _colourHex = _builder.ToString();
    }

    private static void AppendHex(StringBuilder builder, int value)
    {
        builder.Append(HEX_DIGITS[(value >> 4) & 0xF]);
        builder.Append(HEX_DIGITS[value & 0xF]);
    }
}
