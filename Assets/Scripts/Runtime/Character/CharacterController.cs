using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterController : MonoBehaviour, IClickable
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log($"Ho messo il cursore sopra {name}");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"Ho cliccato {name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log($"Avevo il cursore sopra {name}");
    }
}
