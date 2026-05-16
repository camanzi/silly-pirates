using UnityEngine;

public class HealingTouchCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly ITargettable _target;
    private readonly float _healAmount;
    private readonly int _apCost;

    // Undo support
    private float _hpBeforeHeal;

    public HealingTouchCommand(IInteractableElement caster, ITargettable target, float healAmount, int apCost)
    {
        _caster = caster;
        _target = target;
        _healAmount = healAmount;
        _apCost = apCost;
    }

    public async Awaitable ExecuteAsync()
    {
        // Deduct action points
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints -= _apCost;

        // Store current HP for undo
        if (_target is IHealthOwner healthOwner)
        {
            _hpBeforeHeal = healthOwner.Health.CurrentHp;
            healthOwner.Health.Heal(_healAmount);
        }

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        // Restore action points
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints += _apCost;

        // Restore HP to pre-heal value
        if (_target is IHealthOwner healthOwner)
        {
            float delta = healthOwner.Health.CurrentHp - _hpBeforeHeal;
            if (delta > 0f)
                healthOwner.Health.TakeDamage(new DamagePayload(delta));
        }
    }
}
