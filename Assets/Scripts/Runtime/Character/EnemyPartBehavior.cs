using UnityEngine;

[RequireComponent(typeof(OutlinerHelper))]
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(DirectionalSpriteController))]
public class EnemyPartBehavior : MonoBehaviour, ITargettable, IInteractableElement, ISelectable, IHealthOwner
{
    [SerializeField] private SelectionContextSO _selectionContextSO;
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    private OutlinerHelper _outlinerHelper;
    private HealthController _healthController;
    private DirectionalSpriteController _directionalSpriteController;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public HealthController Health => _healthController;

    private void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
    }

    private void OnEnable()
    {
        if (_healthController != null) _healthController.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        if (_healthController != null) _healthController.OnDeath -= OnDeath;
    }

    private void OnDeath()
    {
        _directionalSpriteController?.PlayAnimation(EAnimation.Death);
        _directionalSpriteController?.SetDeadVisual();
    }

    public void OnHoverEnter() => this.HandlePointerEnter();
    public void OnHoverExit() => this.HandlePointerExit();
    public void OnClick() => this.HandlePointerClick();
    public void OnSelectionCtxChange() => this.HandlePointerExit();
}
