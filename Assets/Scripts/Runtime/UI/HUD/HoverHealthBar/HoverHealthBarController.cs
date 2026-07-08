using UnityEngine;
using UnityEngine.UIElements;

public class HoverHealthBarController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;

    private VisualElement _container;
    private VisualElement _fill;
    private Label _nameLabel;
    private HealthController _boundHealth;

    private void Awake() => InitUI();

    private void InitUI()
    {
        if (_container != null) return;
        var root = _hudDocument.rootVisualElement;
        _container = root.Q<VisualElement>("hover-health-container");
        _fill = root.Q<VisualElement>("hover-bar-fill");
        _nameLabel = root.Q<Label>("hover-name-label");
    }

    private void OnDisable() => Unbind();

    public void OnElementHovered(IInteractableElement element)
    {
        InitUI();

        var agent = element as ITurnAgent;
        if (agent?.Health == null || agent.CompareTag("Player"))
        {
            Hide();
            return;
        }

        if (_boundHealth == agent.Health)
        {
            // Already bound to this one, just in case HP already changed.
            RefreshBar();
            return;
        }

        Unbind();
        _boundHealth = agent.Health;
        _boundHealth.OnHpChanged += OnHpChanged;
        _boundHealth.OnDeath += Hide;
        _nameLabel.text = agent.DisplayName;
        _container.style.display = DisplayStyle.Flex;
        RefreshBar();
    }

    private void OnHpChanged(float _) => RefreshBar();

    private void Hide()
    {
        _container.style.display = DisplayStyle.None;
        Unbind();
    }

    private void Unbind()
    {
        if (_boundHealth == null) return;
        _boundHealth.OnHpChanged -= OnHpChanged;
        _boundHealth.OnDeath -= Hide;
        _boundHealth = null;
    }

    private void RefreshBar()
    {
        if (_boundHealth == null) return;
        float ratio = Mathf.Clamp01(_boundHealth.CurrentHp / _boundHealth.MaxHp);
        _fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
    }
}
