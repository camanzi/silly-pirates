using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CrewMemberIndicator : VisualElement
{
    private VisualElement _mainWrapper;
    private VisualElement _portrait;
    private VisualElement _healthBarBg;
    private VisualElement _healthBarFill;
    private Label _nameLabel;
    public ITurnAgent LinkedAgent { get; private set; }

    private Tween _colorTween;
    private Tween _healthTween;

    private VisualElement _passivesContainer;
    private VisualTreeAsset _passiveIndicatorTemplate;
    private PassiveHoverPopupController _hoverPopup;
    private PassiveAbilityController _passiveController;
    private readonly Dictionary<PassiveAbilitySO, PassiveIndicator> _passiveIndicators = new();
    private readonly Dictionary<PassiveAbilitySO, (IPassiveStateNotifier notifier, Action handler)> _passiveSubscriptions = new();
    private readonly HashSet<PassiveAbilitySO> _trackedPassives = new();
    private readonly List<PassiveAbilitySO> _currentPassivesBuffer = new();
    private readonly List<PassiveAbilitySO> _removedPassivesBuffer = new();
    private HealthController _health;

    public void Initialize(ITurnAgent agent, VisualTreeAsset templateAsset, VisualTreeAsset passiveIndicatorTemplate, PassiveHoverPopupController hoverPopup)
    {
        LinkedAgent = agent;
        _passiveIndicatorTemplate = passiveIndicatorTemplate;
        _hoverPopup = hoverPopup;
        templateAsset.CloneTree(this);

        _mainWrapper = this.Q<VisualElement>("main-wrapper");
        _portrait = this.Q<VisualElement>("portrait");
        _healthBarBg = this.Q<VisualElement>("health-bar-bg");
        _healthBarFill = this.Q<VisualElement>("health-bar-fill");
        _passivesContainer = this.Q<VisualElement>("passives-container");
        _nameLabel = this.Q<Label>("name-label");
        _nameLabel.text = agent.DisplayName;

        style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));

        _portrait.style.unityBackgroundImageTintColor = new StyleColor(Color.white);

        if (agent.RenderingData.TurnAgentIcon)
            _portrait.style.backgroundImage = new StyleBackground(agent.RenderingData.TurnAgentIcon);

        _portrait.RegisterCallback<GeometryChangedEvent>(OnPortraitGeometryChanged);

        RegisterCallback<DetachFromPanelEvent>(ev => OnDispose());

        _health = agent.Health;
        if (_health != null)
        {
            _health.OnHpChanged += HandleHpChanged;
            UpdateHealth(_health.CurrentHp, _health.MaxHp);
        }

        if (LinkedAgent is Component comp && comp.TryGetComponent<PassiveAbilityController>(out var controller))
        {
            _passiveController = controller;
            _passiveController.OnPassivesChanged += SyncPassiveIndicators;
            SyncPassiveIndicators();
        }
    }

    public void UpdateTurnState(ITurnAgent activeAgent)
    {
        if (LinkedAgent == null) return;

        bool isMyTurn = LinkedAgent == activeAgent;

        DisplayStyle hpDisplay = isMyTurn ? DisplayStyle.None : DisplayStyle.Flex;
        _healthBarBg.style.display = hpDisplay;
    }

    private void OnPortraitGeometryChanged(GeometryChangedEvent evt)
    {
        _mainWrapper.style.minHeight = _portrait.resolvedStyle.height;
    }

    private void HandleHpChanged(float currentHp)
    {
        if (_health != null)
            UpdateHealth(currentHp, _health.MaxHp);
    }

    private void UpdateHealth(float currentHp, float maxHp)
    {
        bool isDead = currentHp <= 0f;
        float ratio = maxHp > 0f ? currentHp / maxHp : 0f;

        Color targetColor = isDead ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;
        float targetWidth = isDead ? 0f : ratio * 100f;

        _colorTween.Stop();
        _colorTween = Tween.Custom(_portrait, _portrait.resolvedStyle.unityBackgroundImageTintColor, targetColor, duration: 0.3f,
            onValueChange: (target, val) => target.style.unityBackgroundImageTintColor = new StyleColor(val));

        _healthTween.Stop();
        _healthTween = Tween.Custom(_healthBarFill, _healthBarFill.resolvedStyle.width, targetWidth, duration: 0.3f,
            onValueChange: (target, val) => target.style.width = Length.Percent(val));
    }

    private void SyncPassiveIndicators()
    {
        if (_passiveController == null) return;

        _passiveController.GetModifiers(_currentPassivesBuffer);

        foreach (var passive in _currentPassivesBuffer)
        {
            if (_trackedPassives.Contains(passive)) continue;

            _trackedPassives.Add(passive);

            if (passive.Icon == null) continue;

            if (passive is IPassiveStateNotifier notifier)
            {
                PassiveAbilitySO capturedPassive = passive;
                Action handler = () => RefreshPassiveVisibility(capturedPassive);
                notifier.OnStateChanged += handler;
                _passiveSubscriptions[passive] = (notifier, handler);
            }

            RefreshPassiveVisibility(passive);
        }

        _removedPassivesBuffer.Clear();
        foreach (var passive in _trackedPassives)
        {
            if (!_currentPassivesBuffer.Contains(passive))
                _removedPassivesBuffer.Add(passive);
        }

        foreach (var passive in _removedPassivesBuffer)
        {
            _trackedPassives.Remove(passive);

            if (_passiveSubscriptions.TryGetValue(passive, out var subscription))
            {
                subscription.notifier.OnStateChanged -= subscription.handler;
                _passiveSubscriptions.Remove(passive);
            }

            if (_passiveIndicators.TryGetValue(passive, out var existing))
            {
                _passivesContainer.Remove(existing);
                _passiveIndicators.Remove(passive);
            }
        }
    }

    private void RefreshPassiveVisibility(PassiveAbilitySO passive)
    {
        bool shouldShow = passive.IsCurrentlyActive(_passiveController);
        bool isShown = _passiveIndicators.TryGetValue(passive, out var existing);

        if (shouldShow && !isShown)
        {
            var element = new PassiveIndicator(passive, _passiveIndicatorTemplate, _hoverPopup);
            _passivesContainer.Add(element);
            _passiveIndicators[passive] = element;
            existing = element;
            isShown = true;
        }
        else if (!shouldShow && isShown)
        {
            _passivesContainer.Remove(existing);
            _passiveIndicators.Remove(passive);
            isShown = false;
        }

        if (isShown && passive is IStackCountProvider stackProvider)
            existing.UpdateStackBadge(stackProvider.CurrentStacks, stackProvider.MaxStacks);
    }

    private void OnDispose()
    {
        _portrait.UnregisterCallback<GeometryChangedEvent>(OnPortraitGeometryChanged);

        if (_health != null)
        {
            _health.OnHpChanged -= HandleHpChanged;
            _health = null;
        }

        if (_passiveController != null)
            _passiveController.OnPassivesChanged -= SyncPassiveIndicators;

        foreach (var (notifier, handler) in _passiveSubscriptions.Values)
            notifier.OnStateChanged -= handler;
        _passiveSubscriptions.Clear();
    }
}
