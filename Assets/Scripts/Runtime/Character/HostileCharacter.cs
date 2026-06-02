using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(OutlinerHelper))]
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(DirectionalSpriteController))]
public class HostileCharacter : MonoBehaviour, ISelectable, IInteractableElement, ITargettable, ITurnAgent, IHealthOwner, IPartOwner
{
    [Header("General configurations")]
    [SerializeField] private EnemyRole _role;
    [SerializeField] private string _displayName;

    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;
    [SerializeField] private EnemyCritStatsSO _critStats;

    [Header("External references")]
    [SerializeField] private SelectionContextSO _selectionContextSO;
    
    [Header("Event channels")]
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private IntEventChannel _onAPConsumedEventChannel;
    [SerializeField] private AgentAVDeltaEventChannel _onAgilityChangedChannel;

    [Header("Proximity Logic")]
    [SerializeField] protected InteractableProximityEventChannel _proximityChannel;

    [Header("Hostile character configs")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public EnemyRole Role => _role;
    public string DisplayName => _displayName;
    public PassiveAbilityController PassiveAbilityController => _passiveAbilityController;

    public Transform Transform => transform;
    public EnemyCritStatsSO CritStats => _critStats;
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

    public int EffectiveAgility
    {
        get
        {
            if (_passiveAbilityController == null) return AgentData.InitialAgility;
            int totalFlat = 0;
            float totalPct = 0f;
            _passiveAbilityController.GetModifiers(_agilityModifiers);
            foreach (var m in _agilityModifiers)
            {
                totalFlat += m.GetFlatAgilityBonus();
                totalPct  += m.GetPercentageAgilityBonus();
            }
            float modified = (AgentData.InitialAgility + totalFlat) * (1f + totalPct / 100f);
            return Mathf.Max(1, Mathf.RoundToInt(modified));
        }
    }

    public HealthController Health => _healthController;
    
    public readonly List<EnemyAbilityBase> UsedAbilitiesThisTurn = new();
    public readonly Dictionary<EnemyAbilityBase, int> AbilityCooldowns = new();

    private int _remainingActionPoints;
    private int _lastKnownEffectiveAgility;
    private OutlinerHelper _outlinerHelper;
    private HealthController _healthController;
    private EnemyTurnDriver _enemyTurnDriver;
    private EnemyPartController _partController;
    private PassiveAbilityController _passiveAbilityController;
    private DirectionalSpriteController _directionalSpriteController;
    private readonly List<IAgilityModifier> _agilityModifiers = new();
    private readonly List<IOnTurnStart> _turnStartHandlers = new();

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
        _enemyTurnDriver = GetComponent<EnemyTurnDriver>();
        _partController = GetComponent<EnemyPartController>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
    }

    public bool IsPartFunctional(EnemyPartSO part) => _partController == null || _partController.IsPartFunctional(part);
    public Transform GetPartTransform(EnemyPartSO part) => _partController?.GetPartTransform(part);

    protected virtual void OnEnable()
    {
        OnCombatJoin();
        if (_healthController != null)
        {
            _healthController.OnDeath += OnCombatLeave;
            _healthController.OnTakeDamage += OnTakeDamageFeedbackEffect;
            _healthController.OnHealReceived += OnHealReceivedFeedbackEffect;
        }
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged += HandlePassivesChanged;
    }

    protected virtual void OnDisable()
    {
        if (_healthController != null)
        {
            _healthController.OnDeath -= OnCombatLeave;
            _healthController.OnTakeDamage -= OnTakeDamageFeedbackEffect;
            _healthController.OnHealReceived -= OnHealReceivedFeedbackEffect;
        }
        if (_passiveAbilityController != null) _passiveAbilityController.OnPassivesChanged -= HandlePassivesChanged;
    }

    private void Start() => _lastKnownEffectiveAgility = EffectiveAgility;

    private void HandlePassivesChanged()
    {
        int newAgility = EffectiveAgility;
        if (newAgility == _lastKnownEffectiveAgility) return;
        float oldBaseAV = 10000f / Mathf.Max(1, _lastKnownEffectiveAgility);
        float newBaseAV = 10000f / Mathf.Max(1, newAgility);
        _onAgilityChangedChannel?.RaiseEvent(new AgentAVDeltaPayload { Agent = this, AVDelta = newBaseAV - oldBaseAV });
        _lastKnownEffectiveAgility = newAgility;
    }

     public void OnHoverEnter() => this.HandlePointerEnter();

    public void OnClick() => this.HandlePointerClick();

    public void OnHoverExit() => this.HandlePointerExit();

    public void OnSelectionCtxChange() => this.HandlePointerExit();

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave()
    {
        _directionalSpriteController?.PlayAnimation(EAnimation.Death);
        _directionalSpriteController?.SetDeadVisual();
        if (_directionalSpriteController != null)
            _directionalSpriteController.OnAnimationComplete += OnDeathAnimationComplete;
        this.HandleCombatLeave();
    }

    private void OnDeathAnimationComplete(EAnimation anim)
    {
        if (anim != EAnimation.Death) return;
        _directionalSpriteController.OnAnimationComplete -= OnDeathAnimationComplete;
        gameObject.SetActive(false);
    }

    public async void OnStartingTurn()
    {
        var cooldownKeys = new List<EnemyAbilityBase>(AbilityCooldowns.Keys);
        foreach (var k in cooldownKeys)
            if (AbilityCooldowns[k] > 0) AbilityCooldowns[k]--;

        UsedAbilitiesThisTurn.Clear();
        _healthController?.OnTurnStart();
        _partController?.OnTurnStart();
        if (_passiveAbilityController != null)
        {
            _passiveAbilityController.GetModifiers(_turnStartHandlers);
            foreach (var h in _turnStartHandlers) h.OnTurnStart();
        }
        this.HandleStartingTurn();
        this.EmitProximityCheck(ProximityPayload.Empty);
        await _enemyTurnDriver.ExecuteTurnAsync(destroyCancellationToken);
    }

    public async void OnContinuingTurn()
    {
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
