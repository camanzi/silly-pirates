using UnityEngine;
using UnityEngine.UIElements;

public class ActiveAbilityButtonController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private ActiveAbilityRequestEventChannel _activeAbilityRequestChannel;

    private Button _button;
    private AbilityController _cachedAbilityCtrl;
    private ITurnAgent _cachedAgent;
    private readonly ActiveAbilityRequestData _requestData = new();

    private void Start()
    {
        _button = _hudDocument.rootVisualElement.Q<Button>("active-ability-button");
        _button.clicked += OnButtonClicked;
        _button.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.clicked -= OnButtonClicked;
        UnsubscribeFromAgent();
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        UnsubscribeFromAgent();
        if (agent is IAbilityHolder holder && holder.ActiveAbilityController?.ActiveAbility != null)
        {
            _cachedAbilityCtrl = holder.ActiveAbilityController;
            _cachedAgent = agent;
            _button.style.display = DisplayStyle.Flex;
            _cachedAgent.OnAPChanged.OnEventRaised += UpdateButtonEnabledState;
            UpdateButtonEnabledState(agent.RemainingActionPoints);
        }
        else
        {
            _cachedAbilityCtrl = null;
            _button.style.display = DisplayStyle.None;
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
        _button.SetEnabled(remainingAp >= _cachedAbilityCtrl.ActiveAbility.ActionPointCost);
    }

    private void OnButtonClicked()
    {
        if (_cachedAbilityCtrl == null) return;
        _requestData.Ability = _cachedAbilityCtrl.ActiveAbility;
        _requestData.Caster  = _cachedAgent as IInteractableElement;
        _activeAbilityRequestChannel.RaiseEvent(_requestData);
    }
}
