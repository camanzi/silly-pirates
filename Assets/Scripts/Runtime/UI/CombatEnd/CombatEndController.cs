using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Pannello di fine combattimento. Vive nella scena di combattimento, non nella persistente: muore
/// con essa, quindi non ha stato da azzerare fra un combattimento e l'altro.
///
/// Non decide nulla sull'esito: si limita ad ascoltare <see cref="CombatOutcomeStateSO"/>, che è
/// l'unico punto di verità. Chi calcola l'esito è <see cref="CombatOutcomeEvaluator"/>.
///
/// Il blocco degli input non è implementato qui: viene dal fatto che il root è a tutto schermo e
/// pickabile, e che l'oggetto porta anche un UIDocumentRegistrar. WorldInteractor e GridInputHandler
/// consultano già UIPointerTracker.IsPointerOverUI prima di raccogliere un click sul mondo.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CombatEndController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    [Header("Canali")]
    [Tooltip("Riusa il canale di avvio combattimento: partendo da una scena di combattimento già " +
             "caricata, SceneFlowDirector la scarica e la ricarica — che è esattamente 'riprova'.")]
    [SerializeField] private VoidEventChannel _retryRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.35f;
    [SerializeField] private string _victoryText = "Vittoria";
    [SerializeField] private string _defeatText = "Sconfitta";

    private VisualElement _root;
    private Label _title;
    private Button _retryButton;
    private Button _menuButton;
    private Tween _fadeTween;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;

        // Il root del documento copre tutto lo schermo: se restasse pickabile bloccherebbe i click
        // sul mondo per TUTTO il combattimento, non solo quando il pannello è aperto. A bloccare
        // deve essere solo '.combat-end-root', che è nascosto con display:none finché non serve.
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
            // Pull-then-subscribe: se l'esito fosse già risolto quando questo oggetto si abilita,
            // la sola subscribe perderebbe l'evento e il pannello non comparirebbe mai.
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

        // I bottoni vengono riabilitati a ogni apertura: sono disattivati al click per evitare la
        // doppia richiesta, e questo oggetto potrebbe essere riusato se la scena non si ricarica.
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
        // La scena resta viva ancora qualche frame mentre parte la transizione: senza questo, due
        // click rapidi alzerebbero il canale due volte.
        _retryButton?.SetEnabled(false);
        _menuButton?.SetEnabled(false);

        channel?.RaiseEvent();
    }
}
