using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The end-of-combat panel. It lives in the combat scene, not in the persistent one: it dies with it,
/// so it has no state to clear between one combat and the next.
///
/// It decides nothing about the outcome: it merely listens to <see cref="CombatOutcomeStateSO"/>, which
/// is the single source of truth. The one computing the outcome is <see cref="CombatOutcomeEvaluator"/>.
///
/// Input blocking is not implemented here: it comes from the root being full-screen and pickable, and
/// from the object also carrying a UIDocumentRegistrar. WorldInteractor and GridInputHandler already
/// consult UIPointerTracker.IsPointerOverUI before picking up a click on the world.
///
/// The entries are the main menu's <see cref="MenuButton"/>s under a <see cref="MenuSelection"/>, with
/// the same root-owned keyboard/gamepad navigation as <see cref="MainMenuController"/>.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CombatEndController : MonoBehaviour
{
    private const int INITIAL_INDEX = 0; // Restart

    [SerializeField] private UIDocument _document;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    [Header("Channels")]
    [Tooltip("Reuses the combat start channel: starting from an already loaded combat scene, " +
             "SceneFlowDirector unloads and reloads it — which is exactly what 'retry' means.")]
    [SerializeField] private VoidEventChannel _retryRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.35f;
    [SerializeField] private string _victoryText = "Hunt Completed!";
    [SerializeField] private string _defeatText = "Hunt Failed";

    private VisualElement _root;
    private VisualElement _stage;
    private Label _title;
    private MenuButton _retryButton;
    private MenuButton _menuButton;
    private MenuSelection _selection;
    private Tween _fadeTween;
    private bool _transitionRequested;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;

        // The document root covers the whole screen: were it to stay pickable it would block clicks on
        // the world for the WHOLE combat, not just while the panel is open. The only thing that should
        // block is '.combat-end-root', which is hidden with display:none until it is needed.
        documentRoot.pickingMode = PickingMode.Ignore;

        _root = documentRoot.Q<VisualElement>("combat-end-root");
        _stage = documentRoot.Q<VisualElement>("combat-end-stage");
        _title = documentRoot.Q<Label>("combat-end-title");
        SelectionMarker marker = documentRoot.Q<SelectionMarker>("combat-end-marker");
        _retryButton = documentRoot.Q<MenuButton>("retry-button");
        _menuButton = documentRoot.Q<MenuButton>("menu-button");

        Hide();

        if (_retryButton != null) _retryButton.Activated += OnRetryClicked;
        if (_menuButton != null) _menuButton.Activated += OnMenuClicked;

        // Navigation is the only part that needs the whole structure; a partial tree still shows the
        // outcome and keeps whichever entries exist clickable, which beats a dead-end screen.
        if (_root != null && _stage != null && marker != null && _retryButton != null && _menuButton != null)
        {
            _selection = new MenuSelection(marker, new[] { _retryButton, _menuButton });

            _root.focusable = true;
            _root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _stage.RegisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
        }
        else
        {
            Debug.LogError(
                $"[{nameof(CombatEndController)}] the document does not have the expected structure " +
                "(combat-end-root / combat-end-stage / combat-end-marker / retry-button / menu-button): " +
                "keyboard/gamepad navigation is disabled.", this);
        }

        if (_outcomeState != null)
        {
            // Pull-then-subscribe: were the outcome already resolved by the time this object enables,
            // subscribing alone would miss the event and the panel would never appear.
            if (_outcomeState.IsCombatOver) Show(_outcomeState.Outcome);
            _outcomeState.OnCombatResolved += Show;
        }
    }

    private void OnDisable()
    {
        if (_retryButton != null) _retryButton.Activated -= OnRetryClicked;
        if (_menuButton != null) _menuButton.Activated -= OnMenuClicked;
        if (_outcomeState != null) _outcomeState.OnCombatResolved -= Show;

        if (_selection != null)
        {
            _root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _stage.UnregisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
            _selection.Stop();
        }

        _fadeTween.Stop();
    }

    private void Hide()
    {
        if (_root == null) return;

        _root.style.display = DisplayStyle.None;
        _root.style.opacity = 0f;
        // A focused but hidden root would keep routing arrow keys/Submit into an invisible menu.
        _root.Blur();
    }

    private void Show(CombatOutcome outcome)
    {
        if (_root == null || outcome == CombatOutcome.None) return;

        // Same layout and style for both outcomes, as in the mockups: only the words change.
        if (_title != null) _title.text = outcome == CombatOutcome.Defeat ? _defeatText : _victoryText;

        // The buttons are re-enabled on every opening: they are disabled on click to avoid a double
        // request, and this object could be reused if the scene does not reload.
        _retryButton?.SetEnabled(true);
        _menuButton?.SetEnabled(true);
        _transitionRequested = false;

        // Starts on Restart. Not animated, and possibly before the first layout (the root was
        // display:none until the line below): the stage's geometry callback re-places it once the
        // entries have real rectangles.
        _selection?.Select(INITIAL_INDEX, animated: false);

        _fadeTween.Stop();
        _root.style.display = DisplayStyle.Flex;
        // Unscaled for consistency with every other menu, and because the outcome could in principle
        // resolve while something has slowed or frozen time.
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
                static (el, v) => el.style.opacity = v, Ease.OutQuad, useUnscaledTime: true)
            // Focus only once the fade is over: taking it earlier would accept an arrow key or an
            // Enter while the entries are not yet readable on screen. Pointer input is not gated
            // this way, but a click needs the player to see something to click on anyway.
            .OnComplete(_root, static el => el.Focus());
    }

    private void OnStageGeometryChanged(GeometryChangedEvent evt)
    {
        // Going to display:none also reports a change, towards an empty rect: realigning on it would
        // park the quill on zero-sized entries. The next real layout fixes things on its own.
        if (evt.newRect.width <= 0f) return;
        _selection.Refresh();
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
        evt.StopPropagation();

        // SetEnabled(false) only stops pointer clicks: MenuButton.ActivateFromKeyboard checks its
        // Selectable flag, not the enabled state, so this guard is what stops a second Submit.
        if (_transitionRequested) return;

        _selection.ActivateCurrent();
    }

    private void OnRetryClicked() => RequestTransition(_retryRequested);

    private void OnMenuClicked() => RequestTransition(_returnToMenuRequested);

    private void RequestTransition(VoidEventChannel channel)
    {
        // The scene stays alive for a few more frames while the transition starts: without this, two
        // quick clicks (or a click and a Submit) would raise the channel twice.
        if (_transitionRequested) return;
        _transitionRequested = true;

        _retryButton?.SetEnabled(false);
        _menuButton?.SetEnabled(false);

        channel?.RaiseEvent();
    }
}
