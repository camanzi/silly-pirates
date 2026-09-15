using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A label that cycles a trailing "...", one dot at a time, on a fixed interval. Built for the
/// loading screen's "Loading..." caption but not tied to that word: <see cref="BaseText"/> can be
/// anything.
///
/// The four possible strings are pre-built once, when <see cref="BaseText"/> or the panel changes,
/// and the scheduled tick only ever indexes into that array — never concatenates. The dot cycle
/// runs for the entire duration of a loading screen, so a per-tick allocation would be pure waste.
/// </summary>
[UxmlElement]
public partial class EllipsisLabel : Label
{
    private const string USS_CLASS_NAME = "ellipsis-label";

    private string _baseText = "Loading";
    private int _intervalMs = 1000;
    private string[] _states = { "Loading", "Loading.", "Loading..", "Loading..." };
    private IVisualElementScheduledItem _schedule;
    private int _index;

    [UxmlAttribute]
    public string BaseText
    {
        get => _baseText;
        set
        {
            _baseText = value ?? string.Empty;
            _states = new[] { _baseText, _baseText + ".", _baseText + "..", _baseText + "..." };
            ApplyCurrent();
        }
    }

    [UxmlAttribute]
    public int IntervalMs
    {
        get => _intervalMs;
        set
        {
            _intervalMs = Mathf.Max(1, value);
            Rebuild();
        }
    }

    public EllipsisLabel()
    {
        AddToClassList(USS_CLASS_NAME);
        RegisterCallback<AttachToPanelEvent>(_ => Rebuild());
        RegisterCallback<DetachFromPanelEvent>(_ => Stop());
    }

    /// <summary>Restarts from the bare word: resuming from wherever the cycle last stopped would
    /// show the caption reappearing with dots already on it.</summary>
    public void Play()
    {
        _index = 0;
        ApplyCurrent();

        if (_schedule == null) Rebuild();
        _schedule?.Resume();
    }

    public void Stop() => _schedule?.Pause();

    private void Rebuild()
    {
        _schedule?.Pause();
        // The scheduled item is tied to the PANEL, not to this element's visibility: it keeps
        // ticking even under display:none. It is born paused on purpose, and only Play() resumes
        // it, so an off-screen loading screen never spends a timer slot doing nothing useful.
        _schedule = schedule.Execute(Advance).Every(_intervalMs);
        _schedule.Pause();
    }

    private void Advance()
    {
        _index = (_index + 1) % _states.Length;
        ApplyCurrent();
    }

    private void ApplyCurrent() => text = _states[_index];
}
