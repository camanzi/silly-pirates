using UnityEngine;

[CreateAssetMenu(fileName = "Slow Passive", menuName = "Abilities/Character/Passives/Slow")]
public class SlowPassiveSO : PassiveAbilitySO, IAgilityModifier, IOnGlobalTurnStart
{
    [Header("Slow configs")]
    [SerializeField] private int _flatPenalty = 0;
    [SerializeField] private float _percentPenalty = 0f;
    [SerializeField] private int _durationInTurns = 3;

    private PassiveAbilityController _controller;
    private int _turnCount;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnCount = 0;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _controller = null;
    }

    int IAgilityModifier.GetFlatAgilityBonus() => -_flatPenalty;

    float IAgilityModifier.GetPercentageAgilityBonus() => -_percentPenalty;

    void IOnGlobalTurnStart.OnGlobalTurnStart()
    {
        _turnCount++;
        if (_turnCount >= _durationInTurns)
            _controller.RemovePassive(this);
    }
}
