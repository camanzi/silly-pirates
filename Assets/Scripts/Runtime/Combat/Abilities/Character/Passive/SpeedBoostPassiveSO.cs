using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBoost Passive", menuName = "Abilities/Character/Passives/Speed Boost")]
public class SpeedBoostPassiveSO : PassiveAbilitySO, IAgilityModifier, IOnGlobalTurnEnd, IOnTurnStart, IOnTurnEnd
{
    [Header("SpeedBoost configs")]
    [SerializeField] private int _flatBonus = 10;
    [SerializeField] private float _percentBonus = 40f;
    [SerializeField] private int _durationInTurns = 6;

    private static readonly HashSet<HostileCharacter> _activeTargets = new();

    public static bool IsActiveOn(HostileCharacter hostile) => _activeTargets.Contains(hostile);

    private PassiveAbilityController _controller;
    private int _turnCount;
    private bool _isExpired;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnCount  = 0;
        _isExpired  = false;
        if (controller.TryGetComponent<HostileCharacter>(out var h))
            _activeTargets.Add(h);
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<HostileCharacter>(out var h))
            _activeTargets.Remove(h);
        _controller = null;
    }

    int IAgilityModifier.GetFlatAgilityBonus() => _flatBonus;

    float IAgilityModifier.GetPercentageAgilityBonus() => _percentBonus;

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
