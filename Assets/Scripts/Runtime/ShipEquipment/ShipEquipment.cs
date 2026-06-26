using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EquipmentStateMachine))]
public abstract class ShipEquipment : InteractableGridElement, IAwakable, IEquipmentStats, ITargettable, IAbilityHolder
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

    protected override void Awake()
    {
        base.Awake();
        _stateMachine = GetComponent<EquipmentStateMachine>();
        _passiveAbilityController = GetComponent<PassiveAbilityController>();
        _abilityController = GetComponent<AbilityController>();
    }

    public void AddAwakeningPoints(int count)
    {
        int newPoints = Mathf.Min(AwakeningPoints + count, OvercapLimit);
        if (newPoints >= _toAwakePoints && !IsAwake)
            _stateMachine.TransitionTo(new ActiveState(_stateMachine, this));
        AwakeningPoints = newPoints;
    }

    public void RemoveAwakeningPoints(int count)
    {
        int newPoints = Mathf.Max(0, AwakeningPoints - count);
        if (newPoints < _toAwakePoints && IsAwake)
            _stateMachine.TransitionTo(new AwakableState(_stateMachine, this));
        AwakeningPoints = newPoints;
    }

    public void ConsumeAllAwakeningPoints()
    {
        AwakeningPoints = 0;
    }

    public void OnTurnChange(ITurnAgent agent)
    {
        if (!agent.CompareTag("Player")) return;
        _stateMachine.OnTurnChange();
    }
}
