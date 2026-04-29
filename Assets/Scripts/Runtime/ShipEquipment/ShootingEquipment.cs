using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EquipmentStateMachine))]
// FIXME Later, questo dovrá diventare una macchina a stati in piena regola, ma per il momento va bene cosí
public class ShootingEquipment : InteractableGridElement, IAwakable
{
    [Header("Awakable Configs")]
    [SerializeField] private int _toAwakePoints;

    [Header("Feedback Events")]
    [SerializeField] private UnityEvent _onShootEffects;
    public UnityEvent OnShootEffects => _onShootEffects;

    public int MaxAwakeningPoints => _toAwakePoints;
    public int CurrentAwakeningPoints => _awakeningPoints;
    public int AwakeningPoints
    {
        get => _awakeningPoints;
        set
        {
            _awakeningPoints = value;
            OnDataChanged?.Invoke();
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
            if (_cooldown > 0) _stateMachine.TransitionTo(new CooldownState(_stateMachine, this));
        }
    }
    public Action OnDataChanged { get; set; }

    private int _awakeningPoints = 0;
    private int _cooldown = 0;
    private EquipmentStateMachine _stateMachine;

    protected override void Awake()
    {
        base.Awake();
        _stateMachine = GetComponent<EquipmentStateMachine>();
    }

    public void AddAwakeningPoints(int count)
    {
        AwakeningPoints += count;

        if (AwakeningPoints >= _toAwakePoints && !IsAwake)
        {
            _stateMachine.TransitionTo(new ActiveState(_stateMachine, this));
        }
    }

    public void RemoveAwakeningPoints(int count)
    {
        AwakeningPoints = Mathf.Max(0, AwakeningPoints - count);

        if (AwakeningPoints < _toAwakePoints && IsAwake)
        {
            _stateMachine.TransitionTo(new AwakableState(_stateMachine, this));
        }
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
