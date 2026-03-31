using UnityEngine;
using UnityEngine.EventSystems;

public interface IInteractableElement : IClickable 
{
    Transform Transform { get; }
    OutlinerHelper OutlinerHelper { get; }
    SelectionContextSO SelectionContext { get; }
    InteractableElementEventChannel ClickChannel { get; }
}
