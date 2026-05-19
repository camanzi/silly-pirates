using UnityEngine;
using UnityEngine.UIElements;

public class ActiveAbilityButtonController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private ActiveAbilityRequestEventChannel _activeAbilityRequestChannel;

    private ActiveAbilityButton _abilityButton;
    private AbilityController _cachedAbilityCtrl;
    private ITurnAgent _cachedAgent;
    private readonly ActiveAbilityRequestData _requestData = new();

    private void Start()
    {
        _abilityButton = _hudDocument.rootVisualElement.Q<ActiveAbilityButton>("active-ability-button");
        _abilityButton.clicked += OnButtonClicked;
        _abilityButton.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (_abilityButton != null)
            _abilityButton.clicked -= OnButtonClicked;
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

    private void OnButtonClicked()
    {
        if (_cachedAbilityCtrl == null) return;
        _requestData.Ability = _cachedAbilityCtrl.ActiveAbility;
        _requestData.Caster  = _cachedAgent as IInteractableElement;
        _activeAbilityRequestChannel.RaiseEvent(_requestData);
    }
}
