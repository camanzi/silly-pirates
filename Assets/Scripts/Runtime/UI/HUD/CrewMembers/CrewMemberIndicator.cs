using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CrewMemberIndicator : VisualElement
{
    private VisualElement _portrait;
    private VisualElement _healthBarBg;
    private VisualElement _healthBarFill;
    public ITurnAgent LinkedAgent { get; private set; }

    private Tween _scaleTween;
    private Tween _barHeightTween;
    private Tween _colorTween;
    private Tween _healthTween;
    private Tween _marginTween;

    private float _baseMarginLeft;
    private const float NORMAL_BAR_HEIGHT = 20f;
    private const float ACTIVE_BAR_HEIGHT = 25f;
    private const float ACTIVE_MARGIN_OFFSET = 30f;

    public void Initialize(ITurnAgent agent, VisualTreeAsset templateAsset)
    {
        LinkedAgent = agent;
        templateAsset.CloneTree(this);

        _portrait = this.Q<VisualElement>("portrait");
        _healthBarBg = this.Q<VisualElement>("health-bar-bg");
        _healthBarFill = this.Q<VisualElement>("health-bar-fill");

        _baseMarginLeft = resolvedStyle.marginLeft;

        style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));

        style.scale = new Scale(Vector2.one);
        _healthBarBg.style.height = NORMAL_BAR_HEIGHT;
        _portrait.style.unityBackgroundImageTintColor = new StyleColor(Color.white);

        if (agent.RenderingData.TurnAgentIcon)
            _portrait.style.backgroundImage = new StyleBackground(agent.RenderingData.TurnAgentIcon);

        UpdateHealthMock(100);
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

    public void UpdateHealthMock(int currentHealth)
    {
        bool isDead = currentHealth <= 0;
        Color targetColor = isDead ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;
        float targetWidth = isDead ? 0f : 100f;

        _colorTween.Stop();
        _colorTween = Tween.Custom(_portrait, _portrait.style.unityBackgroundImageTintColor.value, targetColor, duration: 0.3f,
            onValueChange: (target, val) => target.style.unityBackgroundImageTintColor = new StyleColor(val));

        _healthTween.Stop();
        _healthTween = Tween.Custom(_healthBarFill, _healthBarFill.style.width.value.value, targetWidth, duration: 0.3f,
            onValueChange: (target, val) => target.style.width = Length.Percent(val));
    }
}