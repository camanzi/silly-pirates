using System.Collections.Generic;

/// <summary>
/// Tracks which menu entry is currently selected and drives the two shared visual signals.
///
/// The two signals behave DIFFERENTLY on purpose:
/// - the quill (<see cref="SelectionMarker"/>) slides from one row to another, so it is a single
///   element that moves;
/// - the underlines do not travel: each <see cref="MenuButton"/> owns and animates its own, so the
///   abandoned entry's line can erase from the right while the newly chosen one writes from the
///   left, in the same instant — two opposite animations that a single shared line could not do.
///
/// The selection is ALWAYS set, even when the pointer is nowhere near the menu: the signals do not
/// appear and disappear with the cursor, they follow whichever entry is active.
///
/// Not a MonoBehaviour: it needs no Unity lifecycle, so it stays owned by whichever screen
/// constructs it (<see cref="MainMenuController"/> or <see cref="TitleScreenPage"/>), which is the
/// only thing that decides when to stop it.
/// </summary>
public sealed class MenuSelection
{
    private readonly SelectionMarker _marker;
    private readonly IReadOnlyList<MenuButton> _buttons;
    private int _index = -1;

    public MenuSelection(SelectionMarker marker, IReadOnlyList<MenuButton> buttons)
    {
        _marker = marker;
        _buttons = buttons;

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            buttons[i].Hovered += () => Select(index, animated: true);
        }
    }

    public int Index => _index;

    public void Select(int index, bool animated)
    {
        if (index < 0 || index >= _buttons.Count) return;
        if (!_buttons[index].Selectable) return;
        if (index == _index && animated) return;

        int previous = _index;
        _index = index;

        _marker.MoveTo(_buttons[index], animated);

        if (previous >= 0 && previous != index) _buttons[previous].SetSelected(false, animated);
        _buttons[index].SetSelected(true, animated);
    }

    /// <summary>
    /// Moves the selection by <paramref name="delta"/> positions, skipping non-selectable entries,
    /// wrapping at the ends. The loop bound is the entry count: without it, a list made entirely of
    /// disabled entries would spin forever.
    /// </summary>
    public void Move(int delta)
    {
        if (_buttons.Count == 0 || delta == 0) return;

        int step = delta > 0 ? 1 : -1;
        int candidate = _index;

        for (int i = 0; i < _buttons.Count; i++)
        {
            candidate = (candidate + step + _buttons.Count) % _buttons.Count;

            if (!_buttons[candidate].Selectable) continue;

            Select(candidate, animated: true);
            return;
        }
    }

    public void ActivateCurrent()
    {
        if (_index < 0 || _index >= _buttons.Count) return;

        _buttons[_index].ActivateFromKeyboard();
    }

    /// <summary>
    /// Realigns the quill and every underline without animating. Needed at the first resolved
    /// layout (when text width becomes measurable) and on every resolution change.
    /// </summary>
    public void Refresh()
    {
        if (_index < 0) return;

        _marker.MoveTo(_buttons[_index], animated: false);

        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].RefreshUnderline(i == _index);
    }

    public void Stop()
    {
        _marker.Stop();

        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].StopUnderline();
    }
}
