using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableGridElement : GridElement, IClickable
{
    [Header("Abilities config")]
    [SerializeField] private AbilityBase _defaultAbility;

    [Header("Event channels")]
    [SerializeField] private AbilitySelectionEventChannel _selectAbilityEventChannel;

    public bool isSelected { 
        get { return _isSelected; } 
        set
        {
            _isSelected = value;
            if (_isSelected)
            {
                _selectAbilityEventChannel.RaiseEvent(new SelectedAbilityPayload(_defaultAbility, this));
            } else
            {
                _selectAbilityEventChannel.RaiseEvent(null);
            }
        }
    }

    protected bool _isSelected;
    protected OutlinerHelper _outlinerHelper;

    protected override void Awake()
    {
        base.Awake();
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _outlinerHelper.AddToOutline();
    }
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        isSelected = !isSelected;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
        {
            RemoveSelectionIndicator();
        }
    }

    public virtual void ExecuteAction(IEnumerable<Vector3Int> calculatedPosition)
    {
        isSelected = false;
        RemoveSelectionIndicator();
    }

    private void RemoveSelectionIndicator()
    {
        _outlinerHelper.RemoveFromOutline();
    }
}
