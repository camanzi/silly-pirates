using UnityEngine;
using UnityEngine.Events;

public abstract class InteractionActionSO : ScriptableObject
{
    [SerializeField] private string _actionName;
    [SerializeField] private Sprite _icon;

    public string ActionName => _actionName;
    public Sprite Icon => _icon;

    public abstract bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent);

    public abstract bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent);

    public virtual bool CanShow(IInteractableElement element, ITurnAgent interactingAgent)
        => CanExecute(element, interactingAgent);

    public virtual int GetHoverApCost(IInteractableElement element, ITurnAgent agent) => 0;
}
