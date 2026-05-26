using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class HostileCharacter : MonoBehaviour, ISelectable, IInteractableElement, ITargettable, ITurnAgent, IHealthOwner, IPartOwner
{
    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;

    [Header("External references")]
    [SerializeField] private SelectionContextSO _selectionContextSO;
    
    [Header("Event channels")]
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private IntEventChannel _onAPConsumedEventChannel;

    [Header("Proximity Logic")]
    [SerializeField] protected InteractableProximityEventChannel _proximityChannel;

    [Header("Hostile character configs")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;
    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;
    public TurnAgentDataSO AgentData => _agentData;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;
    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;
    public IntEventChannel OnAPChanged => _onAPConsumedEventChannel;

    public InteractableProximityEventChannel ProximityChannel => _proximityChannel;
    public int RemainingActionPoints
    {
        get => _remainingActionPoints;
        // Intenzionale NON emettere niente da OnAPConsumed
        set => _remainingActionPoints = value;
    }

    public int EffectiveAgility => AgentData.InitialAgility;

    public HealthController Health => _healthController;
    
    private int _remainingActionPoints;
    private OutlinerHelper _outlinerHelper;
    private HealthController _healthController;
    private EnemyTurnDriver _enemyTurnDriver;
    private EnemyPartController _partController;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
        _enemyTurnDriver = GetComponent<EnemyTurnDriver>();
        _partController = GetComponent<EnemyPartController>();
    }

    public bool IsPartFunctional(EnemyPartSO part)
        => _partController == null || _partController.IsPartFunctional(part);

    protected virtual void OnEnable()
    {
        OnCombatJoin();
        if (_healthController != null)
        {
            _healthController.OnDeath += OnCombatLeave;
            _healthController.OnTakeDamage += OnTakeDamageFeedbackEffect;
            _healthController.OnHealReceived += OnHealReceivedFeedbackEffect;
        }
    }

    protected virtual void OnDisable()
    {
        if (_healthController != null)
        {
            _healthController.OnDeath -= OnCombatLeave;
            _healthController.OnTakeDamage -= OnTakeDamageFeedbackEffect;
            _healthController.OnHealReceived -= OnHealReceivedFeedbackEffect;
        }
    }

     public void OnHoverEnter() => this.HandlePointerEnter();

    public void OnClick() => this.HandlePointerClick();

    public void OnHoverExit() => this.HandlePointerExit();

    public void OnSelectionCtxChange() => this.HandlePointerExit();

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave() => this.HandleCombatLeave();

    public async void OnStartingTurn()
    {
        _healthController?.OnTurnStart();
        _partController?.OnTurnStart();
        this.HandleStartingTurn();
        this.EmitProximityCheck(ProximityPayload.Empty);
        await _enemyTurnDriver.ExecuteTurnAsync(destroyCancellationToken);
    }

    private void OnTakeDamageFeedbackEffect()
    {
        Tween.PunchLocalPosition(_spriteRenderer.transform, strength: Vector3.one * .5f, duration: .5f);
    }

    private void OnHealReceivedFeedbackEffect()
    {
        
    }
}
