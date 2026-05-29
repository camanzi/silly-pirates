using UnityEngine;

[CreateAssetMenu(fileName = "PartRegenBehavior", menuName = "Combat/Health Behaviors/Part Regen")]
public class PartRegenBehaviorSO : HealthBehaviorSO, IOnTurnStart
{
    [SerializeField] private int _regenTurns;

    private HealthController _controller;
    private int _turnsUntilRegen = -1;

    public override void OnEquip(HealthController controller) => _controller = controller;
    public override void OnUnequip(HealthController controller) => _controller = null;

    public override void OnDeath(HealthController controller) => _turnsUntilRegen = _regenTurns;

    void IOnTurnStart.OnTurnStart()
    {
        if (_turnsUntilRegen <= 0) return;
        if (--_turnsUntilRegen == 0)
        {
            _turnsUntilRegen = -1;
            _controller.Revive(_controller.MaxHp);
        }
    }
}
