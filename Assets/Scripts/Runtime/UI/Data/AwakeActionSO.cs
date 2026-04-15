using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AwakeActionSO", menuName = "UI/Interactions/Equipment/Awake Action")]
public class AwakeActionSO : InteractionActionSO
{
    [SerializeField] private int _actionCost;
    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent)
    {
        if (element is not IAwakable awakable) return false;

        awakable.AddAwakingPoints(1);
        return true;
    }
}
