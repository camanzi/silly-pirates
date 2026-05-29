using UnityEngine;
using UnityEngine.UIElements;

public class BossHealthBarController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;

    private VisualElement _container;
    private VisualElement _fill;
    private Label _percentLabel;
    private Label _nameLabel;
    private VisualElement _icon;
    private HealthController _bossHealth;

    private void Awake()
    {
        var root = _hudDocument.rootVisualElement;
        _container = root.Q<VisualElement>("boss-health-container");
        _fill = root.Q<VisualElement>("boss-bar-fill");
        _percentLabel = root.Q<Label>("boss-percent-label");
        _nameLabel = root.Q<Label>("boss-name-label");
        _icon = root.Q<VisualElement>("boss-icon");
    }

    private void OnDisable() => UnbindBoss();

    public void OnAgentJoined(ITurnAgent agent)
    {
        if (agent is not HostileCharacter hostile || !hostile.IsBoss) return;
        UnbindBoss();
        _bossHealth = hostile.Health;
        _bossHealth.OnHpChanged += OnBossHpChanged;
        _bossHealth.OnDeath += OnBossDied;
        _container.style.display = DisplayStyle.Flex;
        _nameLabel.text = hostile.DisplayName;
        if (hostile.RenderingData?.TurnAgentIcon != null)
            _icon.style.backgroundImage = new StyleBackground(hostile.RenderingData.TurnAgentIcon);
        RefreshBar();
    }

    private void OnBossHpChanged(float _) => RefreshBar();

    private void OnBossDied()
    {
        _container.style.display = DisplayStyle.None;
        UnbindBoss();
    }

    private void UnbindBoss()
    {
        if (_bossHealth == null) return;
        _bossHealth.OnHpChanged -= OnBossHpChanged;
        _bossHealth.OnDeath -= OnBossDied;
        _bossHealth = null;
    }

    private void RefreshBar()
    {
        if (_bossHealth == null) return;
        float ratio = Mathf.Clamp01(_bossHealth.CurrentHp / _bossHealth.MaxHp);
        _fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
        _percentLabel.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
    }
}
