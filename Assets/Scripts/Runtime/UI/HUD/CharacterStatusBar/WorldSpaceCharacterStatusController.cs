using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceCharacterStatusController : WorldSpaceContainer
{
    [Header("Character Status")]
    [SerializeField] private HealthController _healthController;
    [SerializeField] private PassiveAbilityController _passiveAbilityController;

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
    }

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
