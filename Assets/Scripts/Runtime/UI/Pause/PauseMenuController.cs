using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The in-combat pause menu. It lives in the combat scene, like the end-of-combat panel: it dies
/// with it, so there is no leftover screen state between one combat and the next.
///
/// It decides nothing about the pause itself: <see cref="PauseStateSO"/> is the single source of
/// truth, and whoever freezes time listens to it. This controller only asks for a state change and
/// reacts to <see cref="PauseStateSO.OnPauseChanged"/> — never to its own buttons directly — so the
/// panel stays in sync even when the pause is toggled by something other than this menu.
///
/// Every tween here runs on unscaled time: the pause sets Time.timeScale to 0, so a scaled tween
/// would sit at its first frame forever and the panel would never fade in. That includes the quill
/// and underline tweens owned by <see cref="SelectionMarker"/> / <see cref="MenuButton"/>, which
/// are unscaled by construction.
///
/// The entries are the main menu's <see cref="MenuButton"/>s, driven by one <see cref="MenuSelection"/>
/// per page: the main page (Resume / Restart / Return) and the restart confirmation, which lives on
/// the same sheet and replaces the entries rather than opening a popup on top of them. Keyboard and
/// gamepad navigation follow <see cref="MainMenuController"/>: the root takes focus and routes
/// NavigationMove/Submit to whichever page is showing.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    // Initial selections. Cancel, not Restart, on the confirmation: an Enter pressed out of habit
    // must land on the non-destructive answer.
    private const int MAIN_INITIAL_INDEX = 0;    // Resume
    private const int CONFIRM_INITIAL_INDEX = 1; // Cancel

    [SerializeField] private UIDocument _document;
    [SerializeField] private PauseStateSO _pauseState;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;
    [SerializeField] private InputReader _inputReader;

    [Header("Channels")]
    [Tooltip("Reuses the combat start channel: starting from an already loaded combat scene, " +
             "SceneFlowDirector unloads it, resets the session and reloads it — which is exactly " +
             "what 'restart' means.")]
    [SerializeField] private VoidEventChannel _restartRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.2f;

    private VisualElement _root;
    private VisualElement _mainPage;
    private VisualElement _confirm;
    private MenuButton _resumeButton;
    private MenuButton _restartButton;
    private MenuButton _menuButton;
    private MenuButton _confirmYesButton;
    private MenuButton _confirmNoButton;

    private MenuSelection _mainSelection;
    private MenuSelection _confirmSelection;

    private Tween _fadeTween;
    private bool _transitionRequested;
    private bool _confirmOpen;

    private MenuSelection ActiveSelection => _confirmOpen ? _confirmSelection : _mainSelection;

    private void OnEnable()
    {
        VisualElement documentRoot = _document != null ? _document.rootVisualElement : null;

        if (documentRoot == null)
        {
            Debug.LogError($"[{nameof(PauseMenuController)}] no UIDocument: the pause menu cannot function.", this);
            return;
        }

        // The document root covers the whole screen: were it to stay pickable it would block clicks
        // on the world for the WHOLE combat, not just while the menu is open. The only thing that
        // should block is '.pause-root', which is hidden with display:none until it is needed.
        documentRoot.pickingMode = PickingMode.Ignore;

        _root = documentRoot.Q<VisualElement>("pause-root");
        _mainPage = documentRoot.Q<VisualElement>("pause-main");
        _confirm = documentRoot.Q<VisualElement>("pause-confirm");
        SelectionMarker mainMarker = documentRoot.Q<SelectionMarker>("pause-marker");
        SelectionMarker confirmMarker = documentRoot.Q<SelectionMarker>("pause-confirm-marker");
        _resumeButton = documentRoot.Q<MenuButton>("pause-resume");
        _restartButton = documentRoot.Q<MenuButton>("pause-restart");
        _menuButton = documentRoot.Q<MenuButton>("pause-menu");
        _confirmYesButton = documentRoot.Q<MenuButton>("pause-confirm-yes");
        _confirmNoButton = documentRoot.Q<MenuButton>("pause-confirm-no");

        if (_root == null || _mainPage == null || _confirm == null
            || mainMarker == null || confirmMarker == null
            || _resumeButton == null || _restartButton == null || _menuButton == null
            || _confirmYesButton == null || _confirmNoButton == null)
        {
            Debug.LogError(
                $"[{nameof(PauseMenuController)}] the document does not have the expected structure " +
                "(pause-root / pause-main / pause-confirm / the two SelectionMarkers / the three " +
                "MenuButton entries / the two confirmation answers): the pause menu stays inert.", this);
            // Nulled so every other method's '_root == null' guard keeps the menu inert, instead of
            // half-working on a partial tree.
            _root = null;
            return;
        }

        // Order of the arrays IS the arrow-key order, top to bottom as drawn.
        _mainSelection = new MenuSelection(mainMarker, new[] { _resumeButton, _restartButton, _menuButton });
        _confirmSelection = new MenuSelection(confirmMarker, new[] { _confirmYesButton, _confirmNoButton });

        // Hidden before the panel draws its first frame: the game does not start paused.
        HideInstant();
        CloseConfirm();

        _resumeButton.Activated += OnResumeClicked;
        _restartButton.Activated += OnRestartClicked;
        _menuButton.Activated += OnMenuClicked;
        _confirmYesButton.Activated += OnConfirmYesClicked;
        _confirmNoButton.Activated += OnConfirmNoClicked;

        _root.focusable = true;
        _root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
        _root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);

        // One callback per page, not one on the stage: the confirmation goes from display:none to
        // visible long after the stage has been laid out, and only ITS geometry change says when its
        // entries finally have rectangles and measurable text.
        _mainPage.RegisterCallback<GeometryChangedEvent>(OnMainGeometryChanged);
        _confirm.RegisterCallback<GeometryChangedEvent>(OnConfirmGeometryChanged);

        if (_inputReader != null) _inputReader.PauseEvent += OnPausePressed;

        if (_pauseState != null)
        {
            // Pull-then-subscribe: were the game already paused by the time this object enables,
            // subscribing alone would leave the panel hidden over a frozen game.
            if (_pauseState.IsPaused) Show();
            _pauseState.OnPauseChanged += HandlePauseChanged;
        }
    }

    private void OnDisable()
    {
        if (_resumeButton != null) _resumeButton.Activated -= OnResumeClicked;
        if (_restartButton != null) _restartButton.Activated -= OnRestartClicked;
        if (_menuButton != null) _menuButton.Activated -= OnMenuClicked;
        if (_confirmYesButton != null) _confirmYesButton.Activated -= OnConfirmYesClicked;
        if (_confirmNoButton != null) _confirmNoButton.Activated -= OnConfirmNoClicked;

        if (_root != null)
        {
            _root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        if (_mainPage != null) _mainPage.UnregisterCallback<GeometryChangedEvent>(OnMainGeometryChanged);
        if (_confirm != null) _confirm.UnregisterCallback<GeometryChangedEvent>(OnConfirmGeometryChanged);

        if (_inputReader != null) _inputReader.PauseEvent -= OnPausePressed;
        if (_pauseState != null) _pauseState.OnPauseChanged -= HandlePauseChanged;

        _fadeTween.Stop();
        _mainSelection?.Stop();
        _confirmSelection?.Stop();
    }

    /// <summary>
    /// Escape / gamepad Start. The key is not a plain toggle: with the confirmation open it means
    /// "back", not "resume", otherwise one press would both dismiss the question and unfreeze the
    /// game.
    /// </summary>
    private void OnPausePressed()
    {
        // A transition is already on its way: the scene lives a few more frames, and re-pausing now
        // would freeze time again with nobody left to unfreeze it.
        if (_transitionRequested) return;

        // The end-of-combat panel already offers restart and return-to-menu: pausing on top of it
        // would stack two menus with the same choices.
        if (_outcomeState != null && _outcomeState.IsCombatOver) return;

        if (_confirmOpen)
        {
            CloseConfirm();
            return;
        }

        if (_pauseState == null) return;
        _pauseState.SetPaused(!_pauseState.IsPaused);
    }

    private void HandlePauseChanged(bool paused)
    {
        if (paused) Show();
        else Hide();
    }

    private void HideInstant()
    {
        if (_root == null) return;

        _root.style.display = DisplayStyle.None;
        _root.style.opacity = 0f;
        // Focus would otherwise survive on a hidden root and keep routing arrow keys/Submit into a
        // menu nobody can see.
        _root.Blur();
    }

    private void Show()
    {
        if (_root == null) return;

        // The confirmation never survives a close/reopen cycle: reopening on the question would be
        // answering something the player never asked again.
        CloseConfirm();

        // Every opening starts on Resume, wherever the quill was left last time. Not animated: the
        // panel is fading in from nothing, there is no previous position worth sliding from. If the
        // page has not been laid out yet (first opening), the geometry callback re-places it.
        _mainSelection.Select(MAIN_INITIAL_INDEX, animated: false);

        // The buttons are re-enabled on every opening: they are disabled on click to avoid a double
        // request, and this object is reused for the whole combat.
        SetButtonsEnabled(true);

        _fadeTween.Stop();
        // Display must be turned on BEFORE the tween: an element in DisplayStyle.None is not drawn
        // at all, so the fade-in would not show.
        _root.style.display = DisplayStyle.Flex;
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.OutQuad, useUnscaledTime: true);

        // Immediately, unlike the end-of-combat screen which waits for its fade: the pause is opened
        // by the player's own key press, so the menu should answer the very next arrow key.
        _root.Focus();
    }

    private void Hide()
    {
        if (_root == null) return;

        _root.Blur();

        _fadeTween.Stop();
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 0f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.InQuad, useUnscaledTime: true)
            // Turning off the display comes after the fade, otherwise the backdrop would stay
            // transparent but clickable and keep stealing clicks on the world.
            .OnComplete(_root, static el => el.style.display = DisplayStyle.None);
    }

    /// <summary>
    /// Swaps the sheet's content to the question. Display, not opacity, for both pages: a hidden
    /// page that stayed pickable would let a hover on an invisible entry steal the selection.
    /// </summary>
    private void OpenConfirm()
    {
        _confirmOpen = true;
        if (_mainPage != null) _mainPage.style.display = DisplayStyle.None;
        if (_confirm != null) _confirm.style.display = DisplayStyle.Flex;

        _confirmSelection?.Select(CONFIRM_INITIAL_INDEX, animated: false);
    }

    private void CloseConfirm()
    {
        _confirmOpen = false;
        if (_confirm != null) _confirm.style.display = DisplayStyle.None;
        if (_mainPage != null) _mainPage.style.display = DisplayStyle.Flex;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _resumeButton?.SetEnabled(enabled);
        _restartButton?.SetEnabled(enabled);
        _menuButton?.SetEnabled(enabled);
        _confirmYesButton?.SetEnabled(enabled);
        _confirmNoButton?.SetEnabled(enabled);
    }

    // --- Layout ----------------------------------------------------------------------------------

    // A page going to display:none also raises a geometry change, towards an empty rect: realigning
    // then would park the quill on zero-sized entries. The next real layout (when the page shows
    // again) raises its own event and does the job properly.
    private void OnMainGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.newRect.width <= 0f) return;
        _mainSelection.Refresh();
    }

    private void OnConfirmGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.newRect.width <= 0f) return;
        _confirmSelection.Refresh();
    }

    // --- Keyboard / gamepad ------------------------------------------------------------------------

    private void OnNavigationMove(NavigationMoveEvent evt)
    {
        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                ActiveSelection.Move(-1);
                break;
            case NavigationMoveEvent.Direction.Down:
                ActiveSelection.Move(1);
                break;
            default:
                return;
        }

        evt.StopPropagation();
    }

    private void OnNavigationSubmit(NavigationSubmitEvent evt)
    {
        evt.StopPropagation();

        // SetEnabled(false) only stops pointer clicks: MenuButton.ActivateFromKeyboard checks its
        // Selectable flag, not the enabled state, so a Submit in the frames before the scene goes
        // away would still get through without this.
        if (_transitionRequested) return;

        ActiveSelection.ActivateCurrent();
    }

    // --- Actions -------------------------------------------------------------------------------------

    // Resume asks the state to change and stops there: the panel closes when OnPauseChanged comes
    // back, so the menu and whoever owns Time.timeScale can never disagree.
    private void OnResumeClicked() => _pauseState?.SetPaused(false);

    // Restarting throws away the whole fight: it goes through a confirmation.
    private void OnRestartClicked() => OpenConfirm();

    private void OnConfirmYesClicked() => RequestTransition(_restartRequested);

    private void OnConfirmNoClicked() => CloseConfirm();

    private void OnMenuClicked() => RequestTransition(_returnToMenuRequested);

    private void RequestTransition(VoidEventChannel channel)
    {
        // The scene stays alive for a few more frames while the transition starts: without this
        // guard, two quick clicks would raise the channel twice.
        if (_transitionRequested) return;
        _transitionRequested = true;
        SetButtonsEnabled(false);

        // Order matters. Unpausing first lets the time controller restore Time.timeScale = 1 while
        // this scene — and the object listening to the pause state — is still alive. Raising the
        // channel first would unload the scene at timeScale 0 and the next one would load frozen.
        _pauseState?.SetPaused(false);

        channel?.RaiseEvent();
    }
}
