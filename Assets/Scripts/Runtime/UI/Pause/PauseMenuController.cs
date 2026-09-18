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
/// would sit at its first frame forever and the panel would never fade in.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
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
    private VisualElement _confirm;
    private Button _resumeButton;
    private Button _restartButton;
    private Button _menuButton;
    private Button _confirmYesButton;
    private Button _confirmNoButton;

    private Tween _fadeTween;
    private bool _transitionRequested;
    private bool _confirmOpen;

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
        _confirm = documentRoot.Q<VisualElement>("pause-confirm");
        _resumeButton = documentRoot.Q<Button>("pause-resume");
        _restartButton = documentRoot.Q<Button>("pause-restart");
        _menuButton = documentRoot.Q<Button>("pause-menu");
        _confirmYesButton = documentRoot.Q<Button>("pause-confirm-yes");
        _confirmNoButton = documentRoot.Q<Button>("pause-confirm-no");

        if (_root == null || _confirm == null || _resumeButton == null || _restartButton == null
            || _menuButton == null || _confirmYesButton == null || _confirmNoButton == null)
        {
            Debug.LogError(
                $"[{nameof(PauseMenuController)}] the document does not have the expected structure " +
                "(pause-root / pause-confirm / the three entries / the two confirmation answers): " +
                "the pause menu stays inert.", this);
            return;
        }

        // Hidden before the panel draws its first frame: the game does not start paused.
        HideInstant();
        CloseConfirm();

        _resumeButton.clicked += OnResumeClicked;
        _restartButton.clicked += OnRestartClicked;
        _menuButton.clicked += OnMenuClicked;
        _confirmYesButton.clicked += OnConfirmYesClicked;
        _confirmNoButton.clicked += OnConfirmNoClicked;

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
        if (_resumeButton != null) _resumeButton.clicked -= OnResumeClicked;
        if (_restartButton != null) _restartButton.clicked -= OnRestartClicked;
        if (_menuButton != null) _menuButton.clicked -= OnMenuClicked;
        if (_confirmYesButton != null) _confirmYesButton.clicked -= OnConfirmYesClicked;
        if (_confirmNoButton != null) _confirmNoButton.clicked -= OnConfirmNoClicked;

        if (_inputReader != null) _inputReader.PauseEvent -= OnPausePressed;
        if (_pauseState != null) _pauseState.OnPauseChanged -= HandlePauseChanged;

        _fadeTween.Stop();
    }

    /// <summary>
    /// Escape / gamepad Start. The key is not a plain toggle: with the confirmation popup open it
    /// means "back", not "resume", otherwise one press would both dismiss the question and unfreeze
    /// the game.
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
    }

    private void Show()
    {
        if (_root == null) return;

        // The popup never survives a close/reopen cycle: reopening on the confirmation question
        // would be answering something the player never asked again.
        CloseConfirm();

        // The buttons are re-enabled on every opening: they are disabled on click to avoid a double
        // request, and this object is reused for the whole combat.
        SetButtonsEnabled(true);

        _fadeTween.Stop();
        // Display must be turned on BEFORE the tween: an element in DisplayStyle.None is not drawn
        // at all, so the fade-in would not show.
        _root.style.display = DisplayStyle.Flex;
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.OutQuad, useUnscaledTime: true);
    }

    private void Hide()
    {
        if (_root == null) return;

        _fadeTween.Stop();
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 0f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.InQuad, useUnscaledTime: true)
            // Turning off the display comes after the fade, otherwise the backdrop would stay
            // transparent but clickable and keep stealing clicks on the world.
            .OnComplete(_root, static el => el.style.display = DisplayStyle.None);
    }

    private void OpenConfirm()
    {
        _confirmOpen = true;
        if (_confirm != null) _confirm.style.display = DisplayStyle.Flex;
    }

    private void CloseConfirm()
    {
        _confirmOpen = false;
        if (_confirm != null) _confirm.style.display = DisplayStyle.None;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _resumeButton?.SetEnabled(enabled);
        _restartButton?.SetEnabled(enabled);
        _menuButton?.SetEnabled(enabled);
        _confirmYesButton?.SetEnabled(enabled);
        _confirmNoButton?.SetEnabled(enabled);
    }

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
