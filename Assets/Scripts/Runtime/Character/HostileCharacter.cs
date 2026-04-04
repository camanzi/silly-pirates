using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class HostileCharacter : MonoBehaviour, ISelectable, IInteractableElement, ITargettable
{

    [SerializeField] private SelectionContextSO _selectionContextSO;
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    public Transform Transform => transform;

    public OutlinerHelper OutlinerHelper => _outlinerHelper;

    public SelectionContextSO SelectionContext => _selectionContextSO;

    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;

    private OutlinerHelper _outlinerHelper;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    public void OnPointerEnter(PointerEventData eventData) => this.HandlePointerEnter();

    public void OnPointerClick(PointerEventData eventData) => this.HandlePointerClick();

    public void OnPointerExit(PointerEventData eventData) => this.HandlePointerExit();

    public void OnSelectionCtxChange() => this.HandlePointerExit();
}
