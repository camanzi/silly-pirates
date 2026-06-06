using UnityEngine;

[CreateAssetMenu(fileName = "Slow Passive", menuName = "Abilities/Character/Passives/Slow")]
public class SlowPassiveSO : PassiveAbilitySO, IAgilityModifier, IOnGlobalTurnEnd, IOnTurnStart, IOnTurnEnd
{
    [Header("Slow configs")]
    [SerializeField] private int _flatPenalty = 0;
    [SerializeField] private float _percentPenalty = 0f;
    [SerializeField] private int _durationInTurns = 3;

    private PassiveAbilityController _controller;
    private int _turnCount;
    private bool _isExpired;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnCount  = 0;
        _isExpired  = false;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _controller = null;
    }

    int IAgilityModifier.GetFlatAgilityBonus() => -_flatPenalty;

    float IAgilityModifier.GetPercentageAgilityBonus() => -_percentPenalty;

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
