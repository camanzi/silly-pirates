using UnityEngine;

[CreateAssetMenu(fileName = "PartRegenBehavior", menuName = "Combat/Health Behaviors/Part Regen")]
public class PartRegenBehaviorSO : HealthBehaviorSO
{
    [SerializeField] private int _regenTurns;

    private int _turnsUntilRegen = -1;

    public override void OnDeath(HealthController controller) => _turnsUntilRegen = _regenTurns;

    public override void OnTurnStart(HealthController controller)
    {
        if (_turnsUntilRegen <= 0) return;
        if (--_turnsUntilRegen == 0)
        {
            _turnsUntilRegen = -1;
            controller.Heal(controller.MaxHp);
        }
    }
}
