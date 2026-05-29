using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealthBarController : WorldSpaceContainer
{
    [Header("Enemy Health Bar Controller Configs")]
    [SerializeField] private HealthController _healthController;

    private VisualElement _fill;

    protected override void Awake()
    {
        base.Awake();
        _fill = _uiDocument.rootVisualElement.Q<VisualElement>("health-bar-fill");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_healthController == null) return;
        _healthController.OnHpChanged += OnHpChanged;
        _healthController.OnDeath += OnEnemyDied;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_healthController == null) return;
        _healthController.OnHpChanged -= OnHpChanged;
        _healthController.OnDeath -= OnEnemyDied;
    }

    protected override void ShowUI() => RefreshBar();
    protected override void RefreshUI() => RefreshBar();

    private void OnHpChanged(float _) => RefreshBar();

    private void OnEnemyDied() => SetElementStatePermission(false);

    private void RefreshBar()
    {
        if (_fill == null || _healthController == null) return;
        float ratio = Mathf.Clamp01(_healthController.CurrentHp / _healthController.MaxHp);
        _fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
    }
}
