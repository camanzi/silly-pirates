using System;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

/// <summary>
/// The title screen: the proxy page that sits in front of the main menu, both on first launch and
/// whenever the player returns to the menu from a match. It is NOT a scene of its own: it is a
/// second stage inside the menu's own document, and it steps aside with a fade to reveal the menu
/// already mounted underneath. This keeps the title -> menu transition off SceneFlowDirector, which
/// would otherwise show the loading overlay for a transition that loads nothing.
///
/// Not a MonoBehaviour, for the same reason as <see cref="MenuSelection"/>: it needs no Unity
/// lifecycle, stays owned by <see cref="MainMenuController"/> (the only thing that decides when to
/// stop it), and there is nothing here that would need wiring up by hand in the Editor.
///
/// The "Press any button" prompt reuses <see cref="MenuSelection"/> with a single entry: it is
/// the same quill + underline pairing as the menu, so it behaves like a selected entry by
/// construction, not merely by resemblance.
/// </summary>
public sealed class TitleScreenPage
{
    private readonly VisualElement _stage;
    private readonly SweepRevealLabel _title;
    private readonly PulsingGroup _promptGroup;
    private readonly MenuSelection _selection;

    private readonly float _promptDelay;
    private readonly float _dismissDuration;

    private Tween _dismiss;
    private IDisposable _anyButton;
    private bool _dismissRequested;

    public TitleScreenPage(VisualElement stage, SweepRevealLabel title, PulsingGroup promptGroup,
        MenuButton promptButton, SelectionMarker promptMarker, float promptDelay, float dismissDuration)
    {
        _stage = stage;
        _title = title;
        _promptGroup = promptGroup;
        _promptDelay = promptDelay;
        _dismissDuration = dismissDuration;

        _selection = new MenuSelection(promptMarker, new[] { promptButton });
        promptButton.Activated += Dismiss;

        // Starting state written immediately, not at the first layout: waiting would show the
        // title already opaque and the prompt already in place for one frame — the end of the
        // animation before its beginning.
        _stage.style.display = DisplayStyle.Flex;
        _stage.style.opacity = 1f;
        _title.Hide();
    }

    /// <summary>
    /// Must be called at resolved layout: before that, text measurement returns zero and every
    /// glyph would collapse to the same point. Returns once the title has been dismissed.
    /// </summary>
    public async Awaitable PlayAsync(CancellationToken token)
    {
        await _title.RevealAsync(token);
        if (token.IsCancellationRequested) return;

        await RevealPromptAsync();
        if (token.IsCancellationRequested) return;

        _promptGroup.StartPulse();
        ArmInput();

        while (!_dismissRequested)
            await Awaitable.NextFrameAsync(token);

        await DismissAsync(token);
    }

    public void Stop()
    {
        _title.Stop();
        _promptGroup.Stop();
        _dismiss.Stop();
        _selection.Stop();

        DisarmInput();
    }

    private async Awaitable RevealPromptAsync()
    {
        // The underline writes left to right and the quill aligns to it: this is the exact same
        // code the menu uses, so the prompt does not merely LOOK like a selected entry, it IS one.
        // PulsingGroup starts Visibility.Hidden, so this cannot be picked/clicked before now.
        _selection.Select(0, animated: true);

        await _promptGroup.FadeInAsync(_promptDelay);
    }

    // --- Input ---------------------------------------------------------------------------------

    /// <summary>
    /// "Any button" is not expressible as an action in the input actions asset, and this project has
    /// no EventSystem to hook into: <c>onAnyButtonPress</c> is the only thing that covers keyboard,
    /// mouse and gamepad without touching GameInput.inputactions.
    ///
    /// Subscribed only NOW, not from the start: a key pressed while the title is still revealing
    /// would otherwise be swallowed silently, skipping the animation with nothing to show for it.
    /// </summary>
    private void ArmInput()
    {
        DisarmInput();
        _anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => Dismiss());
    }

    private void DisarmInput()
    {
        // CallOnce unsubscribes itself on the first press, but if the scene unloads before anyone
        // presses anything, the subscription would otherwise be left hanging off a dead page.
        _anyButton?.Dispose();
        _anyButton = null;
    }

    private void Dismiss() => _dismissRequested = true;

    // --- Farewell --------------------------------------------------------------------------------

    private async Awaitable DismissAsync(CancellationToken token)
    {
        DisarmInput();

        // The pulse must stop BEFORE the fade: it is an infinite tween on the same group, and would
        // keep rewriting opacity while the stage underneath fades away.
        _promptGroup.StopPulse();
        _selection.Stop();

        _dismiss.Stop();
        _dismiss = Tween.Custom(_stage, _stage.resolvedStyle.opacity, 0f,
            Mathf.Max(0.01f, _dismissDuration),
            static (element, value) => element.style.opacity = value, Ease.InQuad,
            useUnscaledTime: true);

        await _dismiss;
        if (token.IsCancellationRequested) return;

        // Actually turned off, not just transparent: full-screen and pickable, it would keep
        // stealing clicks from the menu entries just uncovered.
        _stage.style.display = DisplayStyle.None;
    }
}
