using UnityEngine;

[CreateAssetMenu(fileName = "AwakeActionSO", menuName = "UI/Interactions/Equipment/Awake Action")]
public class AwakeActionSO : InteractionActionSO
{
    [SerializeField] private int _actionCost;

    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;

        var holder = interactingAgent as IAwakeningModifierHolder;
        int bonus = holder?.TotalAwakeningBonus ?? 0;

        if (bonus > 0)
            holder.NotifyAwakeningContribution(awakable);

        awakable.AddAwakeningPoints(1 + bonus);
        interactingAgent.RemainingActionPoints--;
        return true;
    }

    public override bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (interactingAgent == null) return false;
        if (element is not IAwakable awakable || awakable.IsOnCooldown) return false;

        return interactingAgent.RemainingActionPoints > 0
            && !awakable.IsAwake;
    }

    public override bool CanShow(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;
        return !awakable.IsOnCooldown && !awakable.IsAwake;
    }

    public override int GetHoverApCost(IInteractableElement element, ITurnAgent agent) => _actionCost;
}
