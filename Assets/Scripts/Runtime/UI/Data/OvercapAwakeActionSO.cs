using UnityEngine;

[CreateAssetMenu(fileName = "OvercapAwakeActionSO", menuName = "UI/Interactions/Equipment/Overcap Awake Action")]
public class OvercapAwakeActionSO : AwakeActionSO, IInPlaceSwappable
{
    [SerializeField] private AwakeActionSO _baseAction;
    [SerializeField] private AccuracyOvercapPassiveSO _accuracyPassiveTemplate;

    public InteractionActionSO BaseAction => _baseAction;

    public override bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (interactingAgent == null) return false;
        if (element is not IAwakable awakable || awakable.IsOnCooldown) return false;

        return interactingAgent.RemainingActionPoints > 0
            && awakable.IsAwake
            && awakable.CurrentAwakeningPoints < awakable.OvercapLimit;
    }

    public override bool CanShow(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;
        return !awakable.IsOnCooldown
            && awakable.IsAwake
            && awakable.CurrentAwakeningPoints < awakable.OvercapLimit;
    }

    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        bool success = base.ExecuteAction(element, interactingAgent);
        if (!success) return false;

        if (_accuracyPassiveTemplate == null) return true;
        if (element is not IAwakable awakable) return true;
        if (element is not Component comp) return true;
        if (!comp.TryGetComponent<PassiveAbilityController>(out var controller)) return true;

        int extra = Mathf.Max(0, awakable.CurrentAwakeningPoints - awakable.MaxAwakeningPoints);
        int bonus = (int)MathUtils.CalculateOvercapBonus(extra);

        controller.RemovePassive<AccuracyOvercapPassiveSO>();
        var instance = Instantiate(_accuracyPassiveTemplate);
        instance.Initialize(bonus);
        controller.AddPassive(instance);
        return true;
    }
}
