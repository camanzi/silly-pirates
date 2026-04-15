using UnityEngine;
using UnityEngine.Events;

public abstract class InteractionActionSO : ScriptableObject
{
    [SerializeField] private string _actionName;
    [SerializeField] private Sprite _icon;

    public string ActionName => _actionName;
    public Sprite Icon => _icon;

    public abstract bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent);
}
