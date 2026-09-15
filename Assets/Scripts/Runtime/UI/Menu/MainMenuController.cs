using System;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The main menu. Lives in the MainMenu scene, not the persistent one: every scene owns its own UI,
/// so there is no leftover screen state to reset between loads.
///
/// It never loads anything itself: it just raises a channel. Whoever answers is SceneFlowDirector,
/// in the persistent scene, which is the only thing that knows what is currently loaded.
///
/// This is also where the opening sequence is orchestrated, because it is the one place that sees
/// both the illustration (delegated to <see cref="MainMenuDrakeAnimator"/>) and the menu entries:
/// the drake assembles itself, then the entries come in, then input is accepted.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private MainMenuDrakeAnimator _drakeAnimator;
    [SerializeField] private VoidEventChannel _startCombatRequested;

    [Header("Title screen")]
    [Tooltip("Pause between the title text finishing its reveal and the prompt starting to fade in.")]
    [Min(0f)] [SerializeField] private float _titlePromptDelay = 0.2f;

    [Tooltip("Duration of the title stage's fade-out when it steps aside for the menu.")]
    [Min(0.01f)] [SerializeField] private float _titleDismissDuration = 0.35f;

    private VisualElement _root;
    private VisualElement _stage;
    private VisualElement _itemsRoot;
    private SelectionMarker _marker;

    private MenuSelection _selection;
    private TitleScreenPage _titlePage;

    private Sequence _menuEntry;
    private bool _introStarted;
    private bool _transitionRequested;

    private void OnEnable()
    {
        _root = _document != null ? _document.rootVisualElement : null;

        if (_root == null)
        {
            Debug.LogError($"[{nameof(MainMenuController)}] no UIDocument: the menu cannot function.", this);
            return;
        }

        _stage = _root.Q<VisualElement>("menu-stage");
        _itemsRoot = _root.Q<VisualElement>("menu-items");
        _marker = _root.Q<SelectionMarker>("menu-marker");

        MenuButton play = _root.Q<MenuButton>("menu-play");
        MenuButton options = _root.Q<MenuButton>("menu-options");
        MenuButton quit = _root.Q<MenuButton>("menu-quit");

        if (_stage == null || _itemsRoot == null || _marker == null
            || play == null || options == null || quit == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuController)}] the document does not have the expected structure " +
                "(menu-stage / menu-items / menu-marker / the three MenuButton entries): " +
                "the menu stays inert.", this);
            return;
        }

        _selection = new MenuSelection(_marker, new[] { play, options, quit });

        play.Activated += StartCombat;
        quit.Activated += Quit;
        // Options has no listener: it is marked selectable="false" in the UXML, so MenuButton
        // never raises Activated for it and MenuSelection never lands on it either.

        // Hidden before the panel draws its first frame: 'visibility' and not 'display' because the
        // layout must stay measurable — that is where the marker reads where to go and how wide the
        // underline should be. Hidden also blocks picking, so nothing can be clicked during the intro.
        _itemsRoot.style.visibility = Visibility.Hidden;
        _itemsRoot.style.opacity = 0f;
        _marker.style.visibility = Visibility.Hidden;
        _marker.style.opacity = 0f;

        BuildTitlePage();

        _drakeAnimator?.Bind(_stage);

        _root.focusable = true;
        _root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
        _root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);

        // The selection starts on Play and is never cleared: the marker must always be somewhere,
        // even with the mouse resting far from the menu.
        _selection.Select(0, animated: false);

        _stage.RegisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
    }

    private void OnDisable()
    {
        _menuEntry.Stop();
        _selection?.Stop();
        _titlePage?.Stop();

        if (_stage != null) _stage.UnregisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);

        if (_root != null)
        {
            _root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }
    }

    /// <summary>
    /// The title screen is additive on top of the menu: if the document does not contain it, this
    /// logs and moves on, because a menu without an opening page is perfectly playable — while
    /// staying inert would make the game unlaunchable over a courtesy screen.
    /// </summary>
    private void BuildTitlePage()
    {
        VisualElement titleStage = _root.Q<VisualElement>("title-stage");
        SweepRevealLabel titleText = _root.Q<SweepRevealLabel>("title-text");
        PulsingGroup promptGroup = _root.Q<PulsingGroup>("title-prompt-group");
        MenuButton promptButton = _root.Q<MenuButton>("title-prompt-label");
        SelectionMarker promptMarker = _root.Q<SelectionMarker>("title-marker");

        if (titleStage == null || titleText == null || promptGroup == null
            || promptButton == null || promptMarker == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuController)}] the document does not contain the title screen " +
                "(title-stage / title-text / title-prompt-group / title-prompt-label / title-marker): " +
                "it opens directly on the menu.", this);
            return;
        }

        _titlePage = new TitleScreenPage(titleStage, titleText, promptGroup, promptButton, promptMarker,
            _titlePromptDelay, _titleDismissDuration);
    }

    /// <summary>
    /// First layout: only here do the entries' rectangles and the text measurements actually exist.
    /// Before this moment, the entry order and the underline width would be computed on zeros.
    /// </summary>
    private void OnStageGeometryChanged(GeometryChangedEvent evt)
    {
        _selection.Refresh();

        if (_introStarted) return;

        _introStarted = true;
        _ = PlayOpeningAsync(destroyCancellationToken);
    }

    private async Awaitable PlayOpeningAsync(CancellationToken token)
    {
        try
        {
            // The title screen covers everything: the drake must not assemble itself while hidden
            // underneath it, or the illustration's intro would play out unseen.
            if (_titlePage != null)
                await _titlePage.PlayAsync(token);

            if (token.IsCancellationRequested || !isActiveAndEnabled) return;

            if (_drakeAnimator != null)
                await _drakeAnimator.PlayIntroAsync(token);

            if (token.IsCancellationRequested || !isActiveAndEnabled) return;

            MainMenuMotionSO motion = _drakeAnimator != null ? _drakeAnimator.Motion : null;
            float delay = motion != null ? motion.MenuDelay : 0f;
            float duration = motion != null ? motion.MenuDuration : 0.4f;
            Ease ease = motion != null ? motion.MenuEase : Ease.OutQuad;

            _itemsRoot.style.visibility = Visibility.Visible;
            _marker.style.visibility = Visibility.Visible;

            _menuEntry.Stop();
            _menuEntry = Sequence.Create(useUnscaledTime: true);
            _menuEntry = _menuEntry.Insert(delay, Tween.Custom(_itemsRoot, 0f, 1f, duration,
                static (element, value) => element.style.opacity = value, ease, useUnscaledTime: true));
            _menuEntry = _menuEntry.Insert(delay, Tween.Custom(_marker, 0f, 1f, duration,
                static (element, value) => element.style.opacity = value, ease, useUnscaledTime: true));

            await _menuEntry;

            if (token.IsCancellationRequested || !isActiveAndEnabled) return;

            // Focus arrives once the intro is done: taking it earlier would mean accepting an arrow
            // key or an Enter while the entries are not yet on screen.
            _root.Focus();
        }
        catch (OperationCanceledException)
        {
            // Scene unloaded mid-opening: nothing to report.
        }
    }

    private void OnNavigationMove(NavigationMoveEvent evt)
    {
        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                _selection.Move(-1);
                break;
            case NavigationMoveEvent.Direction.Down:
                _selection.Move(1);
                break;
            default:
                return;
        }

        evt.StopPropagation();
    }

    private void OnNavigationSubmit(NavigationSubmitEvent evt)
    {
        _selection.ActivateCurrent();
        evt.StopPropagation();
    }

    private void StartCombat()
    {
        // The scene stays alive for a few more frames while the transition starts: without this
        // guard, a double click would raise the channel twice.
        if (_transitionRequested) return;
        _transitionRequested = true;

        _startCombatRequested?.RaiseEvent();
    }

    private void Quit()
    {
        if (_transitionRequested) return;
        _transitionRequested = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
