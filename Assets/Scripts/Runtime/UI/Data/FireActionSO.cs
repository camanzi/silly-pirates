using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "FireActionSO", menuName = "UI/Interactions/Equipment/Fire Action")]
public class FireActionSO : InteractionActionSO
{
    [SerializeField] private InteractableElementEventChannel _selectedAbilityChannel;
    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not ShootingEquipment shooter) return false;

        _selectedAbilityChannel.RaiseEvent(shooter);
        return true;
    }
}
