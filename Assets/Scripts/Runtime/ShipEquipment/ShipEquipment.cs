using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EquipmentStateMachine))]
public abstract class ShipEquipment : InteractableGridElement, IAwakable, IEquipmentStats, ITargettable
{
    [Header("Equipment Stats")]
    [SerializeField] private EquipmentType _equipmentType;
    [SerializeField] private EquipmentStatsSO _statsConfig;

    public EquipmentType EquipmentType => _equipmentType;
    public EquipmentStatsSO StatsConfig => _statsConfig;

    [Header("Awakable Configs")]
    [SerializeField] private int _toAwakePoints;
    [SerializeField] [Min(0)] private int _maxExtraAwakeningPoints = 2;

    [Header("Feedback Events")]
    [SerializeField] private UnityEvent _onCommandExecuted;
    public UnityEvent OnCommandExecuted => _onCommandExecuted;

    public int MaxAwakeningPoints => _toAwakePoints;
    public int OvercapLimit => _toAwakePoints + _maxExtraAwakeningPoints;
    public int CurrentAwakeningPoints => _awakeningPoints;
    public int AwakeningPoints
    {
        get => _awakeningPoints;
        set
        {
            _awakeningPoints = value;
            OnAwakeningCountersChanged?.Invoke();
        }
    }
    public bool IsAwake => _stateMachine.IsActive;
    public bool IsOnCooldown => _stateMachine.IsOnCooldown;
    public int Cooldown
    {
        get => _cooldown;
        set
        {
            _cooldown = value;
            OnCooldownChanged?.Invoke(_cooldown);
            if (_cooldown > 0) _stateMachine.TransitionTo(new CooldownState(_stateMachine, this));
        }
    }
    public Action OnAwakeningCountersChanged { get; set; }
    public Action<int> OnCooldownChanged { get; set; }

    public PassiveAbilityController PassiveAbilityController => _passiveAbilityController;

    private int _awakeningPoints = 0;
    private int _cooldown = 0;
    private EquipmentStateMachine _stateMachine;
    private PassiveAbilityController _passiveAbilityController;

    protected override void Awake()
    {
        base.Awake();
        _stateMachine = GetComponent<EquipmentStateMachine>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
    }

    public void AddAwakeningPoints(int count)
    {
        AwakeningPoints = Mathf.Min(AwakeningPoints + count, OvercapLimit);

        if (AwakeningPoints >= _toAwakePoints && !IsAwake)
            _stateMachine.TransitionTo(new ActiveState(_stateMachine, this));
    }

    public void RemoveAwakeningPoints(int count)
    {
        AwakeningPoints = Mathf.Max(0, AwakeningPoints - count);

        if (AwakeningPoints < _toAwakePoints && IsAwake)
            _stateMachine.TransitionTo(new AwakableState(_stateMachine, this));
    }

    public void ConsumeAllAwakeningPoints()
    {
        AwakeningPoints = 0;
    }

    public void OnTurnChange()
    {
        _stateMachine.OnTurnChange();
    }
}
