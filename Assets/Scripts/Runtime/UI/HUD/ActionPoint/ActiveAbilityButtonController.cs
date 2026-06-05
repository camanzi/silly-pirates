using UnityEngine;
using UnityEngine.UIElements;

public class ActiveAbilityButtonController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private ActiveAbilityRequestEventChannel _activeAbilityRequestChannel;
    [SerializeField] private IntEventChannel _hoverChannel;

    private ActiveAbilityButton _abilityButton;
    private AbilityController _cachedAbilityCtrl;
    private ITurnAgent _cachedAgent;
    private readonly ActiveAbilityRequestData _requestData = new();

    private void Start()
    {
        _abilityButton = _hudDocument.rootVisualElement.Q<ActiveAbilityButton>("active-ability-button");
        _abilityButton.RegisterCallback<ClickEvent>(OnButtonClicked);
        _abilityButton.RegisterCallback<MouseEnterEvent>(OnMouseHover);
        _abilityButton.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

        _abilityButton.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (_abilityButton != null)
        {
            _abilityButton.UnregisterCallback<ClickEvent>(OnButtonClicked);
            _abilityButton.UnregisterCallback<MouseEnterEvent>(OnMouseHover);
            _abilityButton.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
        UnsubscribeFromAgent();
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        UnsubscribeFromAgent();
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
        _abilityButton.SetEnabled(remainingAp >= _cachedAbilityCtrl.ActiveAbility.ActionPointCost);
    }

    private void OnButtonClicked(ClickEvent clickEvent)
    {
        if (_cachedAbilityCtrl == null) return;
        _requestData.Ability = _cachedAbilityCtrl.ActiveAbility;
        _requestData.Caster  = _cachedAgent as IInteractableElement;
        _activeAbilityRequestChannel.RaiseEvent(_requestData);
    }

    private void OnMouseHover(MouseEnterEvent mouseEnterEvent) => _hoverChannel?.RaiseEvent(_cachedAbilityCtrl.ActiveAbility.ActionPointCost);

    private void OnMouseLeave(MouseLeaveEvent mouseLeaveEvent) => _hoverChannel?.RaiseEvent(0);
}
