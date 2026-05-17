using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AwakeActionSO", menuName = "UI/Interactions/Equipment/Awake Action")]
public class AwakeActionSO : InteractionActionSO
{
    [SerializeField] private int _actionCost;
    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;

        awakable.AddAwakeningPoints(1);

        interactingAgent.RemainingActionPoints--;
        
        return true;
    }

    public override bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (interactingAgent == null) return false;
        if (element is not IAwakable awakable || awakable.IsOnCooldown) return false;

        return interactingAgent.RemainingActionPoints > 0
            && awakable.CurrentAwakeningPoints < awakable.OvercapLimit;
    }

    public override bool CanShow(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;
        return !awakable.IsOnCooldown && awakable.CurrentAwakeningPoints < awakable.OvercapLimit;
    }
}
