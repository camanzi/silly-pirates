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
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CombatEndController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    [Header("Channels")]
    [Tooltip("Reuses the combat start channel: starting from an already loaded combat scene, " +
             "SceneFlowDirector unloads and reloads it — which is exactly what 'retry' means.")]
    [SerializeField] private VoidEventChannel _retryRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.35f;
    [SerializeField] private string _victoryText = "Victory";
    [SerializeField] private string _defeatText = "Defeat";

    private VisualElement _root;
    private Label _title;
    private Button _retryButton;
    private Button _menuButton;
    private Tween _fadeTween;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;

        // The document root covers the whole screen: were it to stay pickable it would block clicks on
        // the world for the WHOLE combat, not just while the panel is open. The only thing that should
        // block is '.combat-end-root', which is hidden with display:none until it is needed.
        documentRoot.pickingMode = PickingMode.Ignore;

        _root = documentRoot.Q<VisualElement>("combat-end-root");
        _title = documentRoot.Q<Label>("combat-end-title");
        _retryButton = documentRoot.Q<Button>("retry-button");
        _menuButton = documentRoot.Q<Button>("menu-button");

        Hide();

        if (_retryButton != null) _retryButton.clicked += OnRetryClicked;
        if (_menuButton != null) _menuButton.clicked += OnMenuClicked;

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
        if (_retryButton != null) _retryButton.clicked -= OnRetryClicked;
        if (_menuButton != null) _menuButton.clicked -= OnMenuClicked;
        if (_outcomeState != null) _outcomeState.OnCombatResolved -= Show;

        _fadeTween.Stop();
    }

    private void Hide()
    {
        if (_root == null) return;

        _root.style.display = DisplayStyle.None;
        _root.style.opacity = 0f;
    }

    private void Show(CombatOutcome outcome)
    {
        if (_root == null || outcome == CombatOutcome.None) return;

        bool isDefeat = outcome == CombatOutcome.Defeat;

        if (_title != null)
        {
            _title.text = isDefeat ? _defeatText : _victoryText;
            _title.EnableInClassList("defeat", isDefeat);
        }

        // The buttons are re-enabled on every opening: they are disabled on click to avoid a double
        // request, and this object could be reused if the scene does not reload.
        _retryButton?.SetEnabled(true);
        _menuButton?.SetEnabled(true);

        _fadeTween.Stop();
        _root.style.display = DisplayStyle.Flex;
        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.OutQuad);
    }

    private void OnRetryClicked() => RequestTransition(_retryRequested);

    private void OnMenuClicked() => RequestTransition(_returnToMenuRequested);

    private void RequestTransition(VoidEventChannel channel)
    {
        // The scene stays alive for a few more frames while the transition starts: without this, two
        // quick clicks would raise the channel twice.
        _retryButton?.SetEnabled(false);
        _menuButton?.SetEnabled(false);

        channel?.RaiseEvent();
    }
}
