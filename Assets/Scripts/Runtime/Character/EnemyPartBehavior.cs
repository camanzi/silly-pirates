using UnityEngine;

[RequireComponent(typeof(OutlinerHelper))]
[RequireComponent(typeof(HealthController))]
public class EnemyPartBehavior : MonoBehaviour, ITargettable, IInteractableElement, ISelectable, IHealthOwner
{
    [SerializeField] private SelectionContextSO _selectionContextSO;
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    private OutlinerHelper _outlinerHelper;
    private HealthController _healthController;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public HealthController Health => _healthController;

    private void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
    }

    public void OnHoverEnter() => this.HandlePointerEnter();
    public void OnHoverExit() => this.HandlePointerExit();
    public void OnClick() => this.HandlePointerClick();
    public void OnSelectionCtxChange() => this.HandlePointerExit();
}
