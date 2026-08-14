using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceCharacterStatusController : WorldSpaceContainer
{
    [Header("Character Status")]
    [SerializeField] private HealthController _healthController;
    [SerializeField] private PassiveAbilityController _passiveAbilityController;

    [Header("Combat Intro")]
    [Tooltip("Se non assegnato la barra si comporta come oggi: nessuna regressione nelle scene senza CombatIntroSequencer.")]
    [SerializeField] private CombatIntroStateSO _introState;
    [Tooltip("Se non assegnato la barra si comporta come oggi: nessuna regressione nelle scene senza CombatIntroSequencer.")]
    [SerializeField] private VoidEventChannel _onCombatStarted;

    private VisualElement _fill;
    private VisualElement _settledPassives;
    private VisualElement _floatingIconsZone;

    protected override void Awake()
    {
        base.Awake();
        var root = _uiDocument.rootVisualElement;
        _fill              = root.Q<VisualElement>("health-bar-fill");
        _settledPassives   = root.Q<VisualElement>("settled-passives");
        _floatingIconsZone = root.Q<VisualElement>("floating-icons-zone");

        // Durante l'intro la barra deve restare invisibile fino a OnCombatStartedEventChannel: si usa
        // lo slot "combat state" (non "element state", riservato alla morte della parte in
        // EnemyPartController) e si applica subito senza tween, altrimenti base.Awake() avrebbe già
        // mostrato il container a piena opacità per un frame.
        if (_introState != null && _introState.IsIntroActive)
        {
            _isAllowedByCombatState = false;
            ApplyVisibilityImmediate();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_healthController != null)
        {
            _healthController.OnHpChanged += OnHpChanged;
            _healthController.OnDeath     += OnDied;
        }
        if (_passiveAbilityController != null)
            _passiveAbilityController.OnPassivesChanged += OnPassivesChanged;

        if (_onCombatStarted != null)
            _onCombatStarted.OnEventRaised += HandleCombatStarted;

        // Copre sia le riattivazioni dopo il combat start sia le scene senza sequencer di intro:
        // in entrambi i casi il permesso di combattimento deve essere concesso da subito.
        if (_introState == null || !_introState.IsIntroActive)
            SetCombatStatePermission(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_healthController != null)
        {
            _healthController.OnHpChanged -= OnHpChanged;
            _healthController.OnDeath     -= OnDied;
        }
        if (_passiveAbilityController != null)
            _passiveAbilityController.OnPassivesChanged -= OnPassivesChanged;

        if (_onCombatStarted != null)
            _onCombatStarted.OnEventRaised -= HandleCombatStarted;
    }

    // Fade-in standard (.25s, gestito dalla base class) quando la HUD compare.
    private void HandleCombatStarted() => SetCombatStatePermission(true);

    protected override void ShowUI()    => RefreshBar();
    protected override void RefreshUI() => RefreshBar();

    private void OnHpChanged(float _) => RefreshBar();
    private void OnDied()              => SetElementStatePermission(false);

    private void RefreshBar()
    {
        if (_fill == null || _healthController == null) return;
        float ratio = Mathf.Clamp01(_healthController.CurrentHp / _healthController.MaxHp);
        _fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
    }

    // TODO: implementare quando PassiveAbilityController esporrà eventi granulari (OnPassiveAdded/Removed)
    private void OnPassivesChanged()
    {
        Debug.Log("[WorldSpaceCharacterStatusController] passive changed — da implementare");
    }
}
