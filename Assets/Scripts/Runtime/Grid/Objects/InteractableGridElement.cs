using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(OutlinerHelper))]
public class InteractableGridElement : GridElement, ISelectable, IInteractableElement
{
    [Header("Dependencies")]
    [SerializeField] private SelectionContextSO _selectionContext;

    [Header("Event channels")]
    [SerializeField] protected InteractableElementEventChannel _elementClickedChannel;

    [Header("Proximity Logic")]
    [SerializeField] protected InteractableProximityEventChannel _proximityChannel;
    [Space]
    [SerializeField] private UnityEvent<bool> OnProximityChanged;
    protected AbilityController AbilityController;
    public AbilityBase DefaultCharacterAbility => AbilityController.DefaultAbility;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;

    public SelectionContextSO SelectionContext => _selectionContext;

    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;

    protected OutlinerHelper _outlinerHelper;

    protected override void Awake()
    {
        base.Awake();
        AbilityController = GetComponent<AbilityController>();
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public void CheckProximity(ProximityPayload payload)
    {
        int distance = 0;
        if (!payload.Equals(ProximityPayload.Empty))
        {
            distance = PathFindingUtils.GetNormalizedDistance(gridPosition, payload.ActiveCharacter.gridPosition);
        }

        bool isInside = distance <= payload.Range;
        
        OnProximityChanged?.Invoke(isInside);
    }

    public void OnHoverEnter() => this.HandlePointerEnter();

    public void OnClick() => this.HandlePointerClick();

    public void OnHoverExit() => this.HandlePointerExit();
    
    public void OnSelectionCtxChange() => this.HandlePointerExit();
}
