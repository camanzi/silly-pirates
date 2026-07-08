using UnityEngine;

[CreateAssetMenu(fileName = "OvercapAwakeActionSO", menuName = "UI/Interactions/Equipment/Overcap Awake Action")]
public class OvercapAwakeActionSO : AwakeActionSO, IInPlaceSwappable
{
    [SerializeField] private AwakeActionSO _baseAction;

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

        if (element is not IAwakable awakable) return true;
        var template = (element as IEquipmentStats)?.StatsConfig?.OvercapPassiveTemplate;
        if (template == null) return true;
        if (element is not Component comp) return true;
        if (!comp.TryGetComponent<PassiveAbilityController>(out var controller)) return true;

        int extra = Mathf.Max(0, awakable.CurrentAwakeningPoints - awakable.MaxAwakeningPoints);
        int bonus = ((IEquipmentStats)element).StatsConfig.GetOvercapBonus(extra);

        controller.RemovePassiveOfType<IOvercapPassive>();
        var instance = Instantiate(template);
        (instance as IOvercapPassive)?.Initialize(bonus);
        controller.AddPassive(instance);
        return true;
    }
}
