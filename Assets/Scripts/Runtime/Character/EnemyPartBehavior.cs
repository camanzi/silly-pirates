using UnityEngine;

[RequireComponent(typeof(OutlinerHelper))]
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(DirectionalSpriteController))]
public class EnemyPartBehavior : MonoBehaviour, ITargettable, IInteractableElement, ISelectable, IHealthOwner
{
    [SerializeField] private SelectionContextSO _selectionContextSO;
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;
    [SerializeField] private bool _overrideEvasion;
    [SerializeField] private int _baseEvasion;

    private OutlinerHelper _outlinerHelper;
    private HealthController _healthController;
    private DirectionalSpriteController _directionalSpriteController;
    private HostileCharacter _parentHostile;

    public Transform Transform => transform;
    public int EffectiveEvasion => _overrideEvasion ? _baseEvasion : _parentHostile?.EffectiveEvasion ?? 0;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public HealthController Health => _healthController;

    private void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
        _parentHostile = GetComponentInParent<HostileCharacter>();
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
