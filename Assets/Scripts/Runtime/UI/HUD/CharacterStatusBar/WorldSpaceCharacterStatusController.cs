using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceCharacterStatusController : WorldSpaceContainer
{
    [Header("Character Status")]
    [SerializeField] private HealthController _healthController;
    [SerializeField] private PassiveAbilityController _passiveAbilityController;

    [Header("Combat Intro")]
    [Tooltip("When unassigned the bar behaves exactly as it does today: no regression in scenes without a CombatIntroSequencer.")]
    [SerializeField] private CombatIntroStateSO _introState;
    [Tooltip("When unassigned the bar behaves exactly as it does today: no regression in scenes without a CombatIntroSequencer.")]
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

        // During the intro the bar has to stay invisible until OnCombatStartedEventChannel: the "combat
        // state" slot is used (not "element state", which is reserved for the death of the part in
        // EnemyPartController) and applied immediately with no tween, otherwise base.Awake() would already
        // have shown the container at full opacity for one frame.
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

        // Covers both re-activations after the combat start and scenes with no intro sequencer:
        // in either case the combat permission has to be granted right away.
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

    // Standard fade-in (.25s, handled by the base class) for when the HUD appears.
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

    // TODO: implement once PassiveAbilityController exposes granular events (OnPassiveAdded/Removed)
    private void OnPassivesChanged()
    {
        Debug.Log("[WorldSpaceCharacterStatusController] passive changed — da implementare");
    }
}
