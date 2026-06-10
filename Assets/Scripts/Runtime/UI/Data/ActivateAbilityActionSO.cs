using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "ActivateAbilityActionSO", menuName = "UI/Interactions/Equipment/Activate Ability Action")]
public class ActivateAbilityActionSO : InteractionActionSO
{
    [SerializeField] private InteractableElementEventChannel _selectedAbilityChannel;
    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not ShipEquipment shooter) return false;

        _selectedAbilityChannel.RaiseEvent(shooter);
        return true;
    }

    public override bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;

        return awakable.IsAwake;
    }

    public override int GetHoverApCost(IInteractableElement element, ITurnAgent agent)
    {
        if (element is IAbilityHolder holder)
            return holder.ActiveAbilityController?.ActiveAbility?.ActionPointCost ?? 0;
        return 0;
    }
}
