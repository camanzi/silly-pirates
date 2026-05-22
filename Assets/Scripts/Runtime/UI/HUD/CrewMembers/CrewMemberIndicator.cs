using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CrewMemberIndicator : VisualElement
{
    private VisualElement _portrait;
    private VisualElement _healthBarBg;
    private Label _healthLabel;
    private VisualElement _healthBarFill;
    public ITurnAgent LinkedAgent { get; private set; }

    private Tween _scaleTween;
    private Tween _barHeightTween;
    private Tween _colorTween;
    private Tween _marginTween;

    private float _baseMarginLeft;
    private const float NORMAL_BAR_HEIGHT = 20f;
    private const float ACTIVE_BAR_HEIGHT = 25f;
    private const float ACTIVE_MARGIN_OFFSET = 30f;

    private VisualElement _passivesContainer;
    private List<IDisposableUI> _disposableElements = new List<IDisposableUI>();
    private HealthController _health;

    public void Initialize(ITurnAgent agent, VisualTreeAsset templateAsset)
    {
        LinkedAgent = agent;
        templateAsset.CloneTree(this);

        _portrait = this.Q<VisualElement>("portrait");
        _healthBarBg = this.Q<VisualElement>("health-bar-bg");
        _healthBarFill = this.Q<VisualElement>("health-bar-fill");
        _healthLabel = this.Q<Label>("health-label");
        _passivesContainer = this.Q<VisualElement>("passives-container");

        _baseMarginLeft = resolvedStyle.marginLeft;

        style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));

        style.scale = new Scale(Vector2.one);
        _healthBarBg.style.height = NORMAL_BAR_HEIGHT;
        _portrait.style.unityBackgroundImageTintColor = new StyleColor(Color.white);

        if (agent.RenderingData.TurnAgentIcon)
            _portrait.style.backgroundImage = new StyleBackground(agent.RenderingData.TurnAgentIcon);

        RegisterCallback<DetachFromPanelEvent>(ev => OnDispose());

        _health = agent.Health;
        if (_health != null)
        {
            _health.OnHpChanged += HandleHpChanged;
            UpdateHealth(_health.CurrentHp, _health.MaxHp);
        }

        InjectPassiveUI();
    }

    public void UpdateTurnState(ITurnAgent activeAgent)
    {
        if (LinkedAgent == null) return;

        bool isMyTurn = LinkedAgent == activeAgent;
        
        Vector3 targetScale = isMyTurn ? new Vector2(1.5f, 1.5f) : Vector2.one;
        float targetMargin = isMyTurn ? (_baseMarginLeft + ACTIVE_MARGIN_OFFSET) : _baseMarginLeft;
        
        _scaleTween.Stop();
        _scaleTween = Tween.Custom(this, style.scale.value.value, targetScale, duration: 0.25f, ease: Ease.OutBack, 
            onValueChange: (target, val) => target.style.scale = new Scale(val));

        _marginTween.Stop();
        _marginTween = Tween.Custom(this, resolvedStyle.marginLeft, targetMargin, duration: 0.25f, ease: Ease.OutQuad,
            onValueChange: (target, val) => {
                target.style.marginLeft = val;
                target.style.marginRight = val;
            });

        float targetBarHeight = isMyTurn ? ACTIVE_BAR_HEIGHT : NORMAL_BAR_HEIGHT;
        _barHeightTween.Stop();
        _barHeightTween = Tween.Custom(_healthBarBg, _healthBarBg.style.height.value.value, targetBarHeight, duration: 0.25f, ease: Ease.OutQuad,
            onValueChange: (target, val) => target.style.height = val);
    }

    private void HandleHpChanged(float currentHp)
    {
        if (_health != null)
            UpdateHealth(currentHp, _health.MaxHp);
    }

    private void UpdateHealth(float currentHp, float maxHp)
    {
        _healthLabel.text = ((int)currentHp).ToString();
        bool isDead = currentHp <= 0f;
        float ratio = maxHp > 0f ? currentHp / maxHp : 0f;

        Color targetColor = isDead ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;
        float targetWidth = isDead ? 0f : ratio * 100f;

        _colorTween.Stop();
        _colorTween = Tween.Custom(_portrait, _portrait.resolvedStyle.unityBackgroundImageTintColor, targetColor, duration: 0.3f,
            onValueChange: (target, val) => target.style.unityBackgroundImageTintColor = new StyleColor(val));

        _healthBarFill.style.width = Length.Percent(targetWidth);
    }

    private void InjectPassiveUI() 
    {
        if (LinkedAgent is Component comp && comp.TryGetComponent<PassiveAbilityController>(out var controller))
        {
            var uiProviders = controller.GetModifiers<IPassiveUIProvider>();
            
            foreach (var provider in uiProviders)
            {
                var element = provider.CreateUIElement(provider.GetTemplate());
                _passivesContainer.Add(element);

                if (element is IDisposableUI disposable)
                {
                    _disposableElements.Add(disposable);
                }
            }
        }
    }

    private void OnDispose()
    {
        if (_health != null)
        {
            _health.OnHpChanged -= HandleHpChanged;
            _health = null;
        }

        foreach (var disp in _disposableElements) disp.Dispose();
        _disposableElements.Clear();
    }
}