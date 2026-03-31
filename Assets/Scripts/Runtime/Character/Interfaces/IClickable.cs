using UnityEngine;
using UnityEngine.EventSystems;

public interface IClickable: IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    // public void SetVisualSelection(bool active);
}
