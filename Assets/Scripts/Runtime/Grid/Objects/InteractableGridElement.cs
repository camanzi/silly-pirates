using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class InteractableGridElement : GridElement, IClickable
{

    [Header("Event channels")]
    [SerializeField] private GridElementEventChannel _selectedGridElementEventChannel;
    [SerializeField] private GridElementEventChannel _deselectedCharacterEventChannel;

    public bool isSelected { 
        get { return _isSelected; } 
        set
        {
            _isSelected = value;
            if (_isSelected)
            {
                _selectedGridElementEventChannel.RaiseEvent(this);
            } else
            {
                _deselectedCharacterEventChannel.RaiseEvent(this);
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

    public virtual void ExecuteAction(Vector3Int targetPosition)
    {
        isSelected = false;
        RemoveSelectionIndicator();
    }

    private void RemoveSelectionIndicator()
    {
        _outlinerHelper.RemoveFromOutline();
    }
}
