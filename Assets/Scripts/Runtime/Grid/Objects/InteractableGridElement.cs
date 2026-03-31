using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class InteractableGridElement : GridElement, ISelectable, IInteractableElement
{
    [Header("Dependencies")]
    [SerializeField] private SelectionContextSO _selectionContext;

    [Header("Event channels")]
    [SerializeField] private AbilitySelectionEventChannel _selectAbilityEventChannel;
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    private AbilityController _abilityController;
    public AbilityBase defaultCharacterAbility => _abilityController.defaultAbility;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;

    public SelectionContextSO SelectionContext => _selectionContext;

    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;

    protected OutlinerHelper _outlinerHelper;

    protected override void Awake()
    {
        base.Awake();
        _abilityController = GetComponent<AbilityController>();
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public void OnPointerEnter(PointerEventData eventData) => this.HandlePointerEnter();

    public void OnPointerClick(PointerEventData eventData) => this.HandlePointerClick();

    public void OnPointerExit(PointerEventData eventData) => this.HandlePointerExit();
    
    // Chiamata da listenere
    public void OnSelectionCtxChange() => this.HandlePointerExit();
}
