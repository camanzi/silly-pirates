using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveAbilityButtonController : MonoBehaviour
{
    private const float HOVER_SCALE = 1.25f;
    private const float SCALE_DURATION = 0.15f;
    private const string USS_CLASS_SELECTED = "active-ability-button--selected";

    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private ActiveAbilityRequestEventChannel _activeAbilityRequestChannel;
    [SerializeField] private IntEventChannel _hoverChannel;
    [SerializeField] private BoolEventChannel _targetingStateChannel;

    private ActiveAbilityButton _abilityButton;
    private AbilityGlowElement _glow;
    private AbilityController _cachedAbilityCtrl;
    private ITurnAgent _cachedAgent;
    private readonly ActiveAbilityRequestData _requestData = new();

    private bool _isHovered;
    private bool _isSelected;
    private Tween _scaleTween;

    private void Start()
    {
        var root = _hudDocument.rootVisualElement;
        _abilityButton = root.Q<ActiveAbilityButton>("active-ability-button");
        _glow = root.Q<AbilityGlowElement>("active-ability-glow");

        _abilityButton.RegisterCallback<ClickEvent>(OnButtonClicked);
        _abilityButton.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _abilityButton.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        if (_targetingStateChannel != null)
            _targetingStateChannel.OnEventRaised += OnTargetingStateChanged;

        _abilityButton.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (_abilityButton != null)
        {
            _abilityButton.UnregisterCallback<ClickEvent>(OnButtonClicked);
            _abilityButton.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            _abilityButton.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        if (_targetingStateChannel != null)
            _targetingStateChannel.OnEventRaised -= OnTargetingStateChanged;

        _scaleTween.Stop();
        _glow?.StopAll();
        UnsubscribeFromAgent();
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        if (_abilityButton == null) return;

        UnsubscribeFromAgent();
        ResetVisualState();

        if (agent is IAbilityHolder holder && holder.ActiveAbilityController?.ActiveAbility != null)
        {
            _cachedAbilityCtrl = holder.ActiveAbilityController;
            _cachedAgent = agent;
            _abilityButton.style.display = DisplayStyle.Flex;
            _abilityButton.SetAbilityIcon(_cachedAbilityCtrl.ActiveAbility.Icon);
            _cachedAgent.OnAPChanged.OnEventRaised += UpdateButtonEnabledState;
            UpdateButtonEnabledState(agent.RemainingActionPoints);
        }
        else
        {
            _cachedAbilityCtrl = null;
            _abilityButton.SetAbilityIcon(null);
            _abilityButton.style.display = DisplayStyle.None;
        }
    }

    private void UnsubscribeFromAgent()
    {
        if (_cachedAgent != null)
            _cachedAgent.OnAPChanged.OnEventRaised -= UpdateButtonEnabledState;
        _cachedAgent = null;
    }

    private void UpdateButtonEnabledState(int remainingAp)
    {
        if (_cachedAbilityCtrl?.ActiveAbility == null) return;

        bool canAfford = remainingAp >= _cachedAbilityCtrl.ActiveAbility.ActionPointCost;
        _abilityButton.SetEnabled(canAfford);

        // A greyed-out button must not keep inviting interaction.
        if (!canAfford && _isHovered)
        {
            _isHovered = false;
            ApplyScale();
        }
    }

    private void OnButtonClicked(ClickEvent clickEvent)
    {
        if (_cachedAbilityCtrl?.ActiveAbility == null) return;

        var ability = _cachedAbilityCtrl.ActiveAbility;
        _requestData.Ability = ability;
        _requestData.Caster  = _cachedAgent as IInteractableElement;
        _activeAbilityRequestChannel.RaiseEvent(_requestData);

        if (ability.RequiresTargeting)
            SetSelected(true);
        else
            _glow?.Flash();
    }

    /// <summary>
    /// Raised by <see cref="TargetingStateSO"/> on enter/exit. Only the exit matters here:
    /// selection is entered exclusively through this button's own click, so targeting started
    /// from other UI (interaction menu, portrait click) never lights up this glow.
    /// </summary>
    private void OnTargetingStateChanged(bool isTargeting)
    {
        if (!isTargeting && _isSelected)
            SetSelected(false);
    }

    private void SetSelected(bool selected)
    {
        _isSelected = selected;
        _abilityButton.EnableInClassList(USS_CLASS_SELECTED, selected);
        ApplyScale();

        if (selected) _glow?.PlayIn();
        else _glow?.PlayOut();
    }

    private void ApplyScale()
    {
        float target = _isHovered || _isSelected ? HOVER_SCALE : 1f;
        float current = _abilityButton.resolvedStyle.scale.value.x;
        if (Mathf.Approximately(current, target)) return;

        _scaleTween.Stop();
        _scaleTween = Tween.Custom(_abilityButton, current, target, SCALE_DURATION,
            ease: target > 1f ? Ease.OutBack : Ease.OutQuad,
            onValueChange: (t, val) => t.style.scale = new StyleScale(new Scale(new Vector3(val, val, 1f))));
    }

    private void ResetVisualState()
    {
        _isSelected = false;
        _isHovered = false;
        _abilityButton.RemoveFromClassList(USS_CLASS_SELECTED);
        _scaleTween.Stop();
        _abilityButton.style.scale = new StyleScale(new Scale(Vector3.one));
        _glow?.StopAll();
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (_cachedAbilityCtrl?.ActiveAbility == null || !_abilityButton.enabledInHierarchy) return;

        _isHovered = true;
        ApplyScale();
        _hoverChannel?.RaiseEvent(_cachedAbilityCtrl.ActiveAbility.ActionPointCost);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _isHovered = false;
        ApplyScale();
        _hoverChannel?.RaiseEvent(0);
    }
}
