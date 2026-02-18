using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterController : MonoBehaviour, IClickable
{
    private OutlinerHelper _outlinerHelper;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _outlinerHelper.AddToOutline();
        // Debug.Log($"Ho messo il cursore sopra {name}");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"Ho cliccato {name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _outlinerHelper.RemoveFromOutline();
        // Debug.Log($"Avevo il cursore sopra {name}");
    }
}
