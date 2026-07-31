using System;
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
    public CharacterLifecycleAnimator LifecycleAnimator => _lifecycleAnimator;
    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;
    public TurnAgentDataSO AgentData => _agentData;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;
    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;
    public IntEventChannel OnAPChanged => _onAPConsumedEventChannel;

    public void SetSpawnPoint(SpawnPoint spawnPoint) => _spawnPoint = spawnPoint;

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

    public int EffectiveEvasion
    {
        get
        {
            if (_passiveAbilityController == null) return AgentData.BaseEvasion;
            int total = 0;
            _passiveAbilityController.GetModifiers(_evasionModifiers);
            for (int i = 0; i < _evasionModifiers.Count; i++)
                total += _evasionModifiers[i].GetEvasionBonus();
            return AgentData.BaseEvasion + total;
        }
    }

    public HealthController Health => _healthController;

    public int TurnCount { get; private set; }
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
    private CharacterLifecycleAnimator _lifecycleAnimator;
    private Collider[] _colliders;
    private readonly List<IAgilityModifier> _agilityModifiers = new();
    private readonly List<IEvasionModifier> _evasionModifiers = new();
    private readonly List<IOnTurnStart> _turnStartHandlers = new();
    private SpawnPoint _spawnPoint;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
        _healthController = GetComponent<HealthController>();
        _enemyTurnDriver = GetComponent<EnemyTurnDriver>();
        _partController = GetComponent<EnemyPartController>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
        _lifecycleAnimator = GetComponent<CharacterLifecycleAnimator>();
        _colliders = GetComponentsInChildren<Collider>();
    }

    public bool IsPartFunctional(EnemyPartSO part) => _partController == null || _partController.IsPartFunctional(part);
    public Transform GetPartTransform(EnemyPartSO part) => _partController?.GetPartTransform(part);
    public event Action<EnemyPartSO> OnPartBroken
    {
        add    => _partController.OnPartBroken += value;
        remove => _partController.OnPartBroken -= value;
    }

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

    public void OnCombatJoin()
    {
        SetInteractable(true);                  // ripristina i collider se il GO viene riattivato dopo una morte

        _lifecycleAnimator?.Play(LifecyclePhase.Spawn);
        this.HandleCombatJoin();
    }

    public async void OnCombatLeave()
    {
        _spawnPoint?.Release();
        _spawnPoint = null;

        this.HandleCombatLeave();               // PRIMA di tutto: esce subito dal turn order
        SetInteractable(false);                 // collider + outline off: non cliccabile mentre affonda
        _directionalSpriteController?.SetDeadVisual();

        try
        {
            if (_lifecycleAnimator != null)
                await _lifecycleAnimator.PlayAsync(LifecyclePhase.Leave, destroyCancellationToken);
        }
        catch (OperationCanceledException) { return; }

        if (this == null) return;               // distrutto durante l'animazione
        gameObject.SetActive(false);
    }

    private void SetInteractable(bool interactable)
    {
        if (_colliders != null)
            foreach (var collider in _colliders)
                if (collider != null) collider.enabled = interactable;

        if (!interactable)
            _outlinerHelper?.ClearOutline();
    }

    public async void OnStartingTurn()
    {
        TurnCount++;
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

    public void OnEndingTurn()
    {
        if (_passiveAbilityController != null)
        {
            _passiveAbilityController.HandleOwnerTurnEnd();
        }
    }

    private void OnTakeDamageFeedbackEffect()
    {
        Tween.PunchLocalPosition(_spriteRenderer.transform, strength: Vector3.one * .5f, duration: .5f);
    }

    private void OnHealReceivedFeedbackEffect()
    {
        
    }
}
