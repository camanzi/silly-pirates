using UnityEngine;
using UnityEngine.UIElements;

public class ActiveAbilityButtonController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private ActiveAbilityRequestEventChannel _activeAbilityRequestChannel;

    private Button _button;
    private AbilityController _cachedAbilityCtrl;
    private IInteractableElement _cachedCaster;
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
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        if (agent is IAbilityHolder holder && holder.ActiveAbilityController?.ActiveAbility != null)
        {
            _cachedAbilityCtrl = holder.ActiveAbilityController;
            _cachedCaster = agent as IInteractableElement;
            _button.style.display = DisplayStyle.Flex;
        }
        else
        {
            _cachedAbilityCtrl = null;
            _cachedCaster = null;
            _button.style.display = DisplayStyle.None;
        }
    }

    private void OnButtonClicked()
    {
        if (_cachedAbilityCtrl == null) return;
        _requestData.Ability = _cachedAbilityCtrl.ActiveAbility;
        _requestData.Caster  = _cachedCaster;
        _activeAbilityRequestChannel.RaiseEvent(_requestData);
    }
}
