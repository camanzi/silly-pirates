using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Slow Passive", menuName = "Abilities/Character/Passives/Slow")]
public class SlowPassiveSO : PassiveAbilitySO, IAgilityModifier, IOnGlobalTurnEnd, IOnTurnStart, IOnTurnEnd, IStackCountProvider, IPassiveStateNotifier, IStackablePassive
{
    [Header("Slow configs")]
    [SerializeField] private int _flatPenalty = 0;
    [SerializeField] private float _percentPenalty = 0f;
    [SerializeField] private int _durationInTurns = 3;

    private PassiveAbilityController _controller;
    private int _turnCount;
    private bool _isExpired;
    private int _stacks;

    public int CurrentStacks => _stacks;
    public int MaxStacks => int.MaxValue;

    public event Action OnStateUpdated;

    event Action IPassiveStateNotifier.OnStateChanged
    {
        add => OnStateUpdated += value;
        remove => OnStateUpdated -= value;
    }

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnCount  = 0;
        _isExpired  = false;
        _stacks     = 1;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _controller = null;
    }

    void IStackablePassive.OnReapplied(PassiveAbilityController controller)
    {
        _stacks++;
        _turnCount = 0;
        _isExpired = false;
        OnStateUpdated?.Invoke();
    }

    int IAgilityModifier.GetFlatAgilityBonus() => -_flatPenalty * _stacks;

    float IAgilityModifier.GetPercentageAgilityBonus() => -_percentPenalty * _stacks;

    void IOnGlobalTurnEnd.OnGlobalTurnEnd()
    {
        _turnCount++;
        if (_turnCount >= _durationInTurns)
        {
            if (RemovalTiming == PassiveRemovalTiming.AnyTurn)
                _controller.RemovePassive(this);
            else
                _isExpired = true;
        }
    }

    void IOnTurnStart.OnTurnStart()
    {
        if (_isExpired && RemovalTiming == PassiveRemovalTiming.OwnerTurnStart)
            _controller.RemovePassive(this);
    }

    void IOnTurnEnd.OnTurnEnd()
    {
        if (_isExpired && RemovalTiming == PassiveRemovalTiming.OwnerTurnEnd)
            _controller.RemovePassive(this);
    }
}
