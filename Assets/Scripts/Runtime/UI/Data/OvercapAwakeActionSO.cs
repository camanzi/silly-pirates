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

        return interactingAgent.RemainingActionPoints >= ActionCost
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

    // ExecuteAction è quella della base: il passivo di overcap lo applica
    // ShipEquipment.RefreshOvercapPassive in base ai punti risultanti.
}
