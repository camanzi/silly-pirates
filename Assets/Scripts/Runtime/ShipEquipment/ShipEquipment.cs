using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EquipmentStateMachine))]
public abstract class ShipEquipment : InteractableGridElement, IAwakable, IEquipmentStats, ITargettable, IAbilityHolder, IMuzzleOwner
{
    [Header("Equipment Stats")]
    [SerializeField] private EquipmentType _equipmentType;
    [SerializeField] private EquipmentStatsSO _statsConfig;

    public EquipmentType EquipmentType => _equipmentType;
    public EquipmentStatsSO StatsConfig => _statsConfig;

    [Header("Awakable Configs")]
    [SerializeField] private int _toAwakePoints;

    [Header("Feedback Events")]
    [SerializeField] private UnityEvent _onCommandExecuted;
    public UnityEvent OnCommandExecuted => _onCommandExecuted;

    [Tooltip("The origin of projectiles and muzzle VFX. When empty a MuzzleAnchor is looked up automatically among the children; failing that, the equipment's root is used.")]
    [SerializeField] private Transform _muzzleAnchor;
    public Transform Muzzle => _muzzleAnchor;

    public int MaxAwakeningPoints => _toAwakePoints;
    public int CurrentAwakeningPoints => _awakeningPoints;
    public bool IsAwake => _stateMachine?.IsActive ?? false;
    public bool IsOnCooldown => _stateMachine?.IsOnCooldown ?? false;
    public int Cooldown
    {
        get => _cooldown;
        set
        {
            _cooldown = value;
            if (_cooldown > 0)
            {
                OnCooldownChanged?.Invoke(_cooldown);
                _stateMachine.TransitionTo(new CooldownState(_stateMachine, this));
            } else
            {
                OnAwakeningCountersChanged?.Invoke();
            }
        }
    }
    public Action OnAwakeningCountersChanged { get; set; }
    public Action<int> OnCooldownChanged { get; set; }
    public Action<int> OnAwakeningHoverPreview { get; set; }

    public PassiveAbilityController PassiveAbilityController => _passiveAbilityController;

    private int _awakeningPoints = 0;
    private int _cooldown = 0;
    private EquipmentStateMachine _stateMachine;
    private PassiveAbilityController _passiveAbilityController;
    private AbilityController _abilityController;

    public AbilityController ActiveAbilityController => _abilityController;

    /// <summary>Resolved in Awake, so it is already available to subclasses in OnEnable.</summary>
    protected EquipmentStateMachine StateMachine => _stateMachine;

    protected override void Awake()
    {
        base.Awake();
        // The serialized field is the manual override: when empty, the marker on the prefab is the source
        // of truth, and it is resolved once and not on every shot.
        if (_muzzleAnchor == null)
            _muzzleAnchor = GetComponentInChildren<MuzzleAnchor>(true)?.transform;
        _stateMachine = GetComponent<EquipmentStateMachine>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _abilityController = GetComponent<AbilityController>();
    }

    /// <summary>
    /// Adds points up to <see cref="MaxAwakeningPoints"/>: anything beyond the threshold is discarded, there
    /// is no overcap. Reaching the threshold awakens the equipment.
    /// </summary>
    public void AddAwakeningPoints(int count)
    {
        int newPoints = Mathf.Min(_awakeningPoints + count, _toAwakePoints);
        if (newPoints >= _toAwakePoints && !IsAwake)
            _stateMachine.TransitionTo(new ActiveState(_stateMachine, this));
        SetAwakeningPoints(newPoints);
    }

    public void RemoveAwakeningPoints(int count)
    {
        int newPoints = Mathf.Max(0, _awakeningPoints - count);
        if (newPoints < _toAwakePoints && IsAwake)
            _stateMachine.TransitionTo(new AwakableState(_stateMachine, this));
        SetAwakeningPoints(newPoints);
    }

    public void ConsumeAllAwakeningPoints() => SetAwakeningPoints(0);

    private void SetAwakeningPoints(int value)
    {
        _awakeningPoints = value;
        OnAwakeningCountersChanged?.Invoke();
    }

    public void OnTurnChange(ITurnAgent agent)
    {
        // Cooldowns tick once per crew turn: neither enemies nor the ship advance them.
        if (!TurnAgentRoles.IsCrewMember(agent)) return;
        _stateMachine.OnTurnChange();
    }
}
