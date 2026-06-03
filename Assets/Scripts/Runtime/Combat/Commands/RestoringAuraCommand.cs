using UnityEngine;

public class RestoringAuraCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly GridCharacter _target;
    private readonly float _reviveHpPercentage;
    private readonly int _cooldown;

    public RestoringAuraCommand(IInteractableElement caster, GridCharacter target, float reviveHpPercentage, int cooldown)
    {
        _caster = caster;
        _target = target;
        _reviveHpPercentage = reviveHpPercentage;
        _cooldown = cooldown;
    }

    public async Awaitable ExecuteAsync()
    {
        if (_caster is IAwakable awakable)
        {
            awakable.ConsumeAllAwakeningPoints();
            awakable.Cooldown = _cooldown;
        }

        _target.Health.Revive(_target.Health.MaxHp * _reviveHpPercentage);

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_target.Health.IsAlive)
            _target.Health.TakeDamage(new DamagePayload(_target.Health.CurrentHp));
    }
}
