using System;
using System.Collections.Generic;
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
            RefreshOvercapPassive();
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

    private readonly List<IEvasionModifier> _evasionModifiers = new();

    public int EffectiveEvasion
    {
        get
        {
            int total = 0;
            if (_passiveAbilityController != null)
            {
                _passiveAbilityController.GetModifiers(_evasionModifiers);
                for (int i = 0; i < _evasionModifiers.Count; i++)
                    total += _evasionModifiers[i].GetEvasionBonus();
            }
            return (StatsConfig?.BaseEvasion ?? 0) + total;
        }
    }

    private int _awakeningPoints = 0;
    private int _appliedOvercapExtra = 0;
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

    // Il bonus di overcap dipende dallo stato dei punti, non da chi li ha aggiunti:
    // qualunque sorgente (azione base, azione overcap, Maximize Contribution) passa di qui.
    private void RefreshOvercapPassive()
    {
        if (_passiveAbilityController == null) return;

        int extra = Mathf.Max(0, _awakeningPoints - _toAwakePoints);
        if (extra == _appliedOvercapExtra) return;
        _appliedOvercapExtra = extra;

        if (extra <= 0)
        {
            _passiveAbilityController.RemovePassiveOfType<IOvercapPassive>();
            return;
        }

        var template = _statsConfig?.OvercapPassiveTemplate;
        if (template == null) return;

        // AddPassive gestisce la riapplicazione: l'istanza esistente sovrascrive il proprio
        // bonus con quello ricalcolato dal totale e questa copia viene distrutta.
        var instance = Instantiate(template);
        (instance as IOvercapPassive)?.Initialize(_statsConfig.GetOvercapBonus(extra));
        _passiveAbilityController.AddPassive(instance);
    }

    public void OnTurnChange(ITurnAgent agent)
    {
        if (!agent.CompareTag("Player")) return;
        _stateMachine.OnTurnChange();
    }
}
