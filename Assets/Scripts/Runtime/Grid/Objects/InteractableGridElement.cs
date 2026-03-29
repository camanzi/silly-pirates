using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class InteractableGridElement : GridElement, IClickable
{
    [Header("Dependencies")]
    [SerializeField] private SelectionContextSO _selectionContext;

    [Header("Event channels")]
    [SerializeField] private AbilitySelectionEventChannel _selectAbilityEventChannel;
    [SerializeField] private GridElementEventChannel _elementClickedChannel;

    private AbilityController _abilityController;
    public AbilityBase defaultCharacterAbility => _abilityController.defaultAbility;
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

    public void SetVisualSelection(bool active)
    {
        if (active)
        {
            _outlinerHelper.AddToOutline();
        }  else
        {
            _outlinerHelper.RemoveFromOutline();
        } 
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _outlinerHelper.AddToOutline();
    }
    public virtual void OnPointerClick(PointerEventData eventData)
    {   
        _elementClickedChannel.RaiseEvent(this);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        UpdateOutlineStatus();
    }

    public void UpdateOutlineStatus()
    {
        if (!IsSelectedBySystem())
        {
            _outlinerHelper.RemoveFromOutline();
        }
    }

    private bool IsSelectedBySystem()
    {
        if (_selectionContext == null) return false;
        return _selectionContext.IsElementSelected(this);
    }
}
