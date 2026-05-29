using PrimeTween;
using UnityEngine;

public class HealingWaterCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly ITargettable _target;
    private readonly float _healAmount;
    private readonly GameObject _projectilePrefab;
    private readonly TrajectoryConfigsSO _trajectoryConfig;

    private float _hpBeforeHeal;

    public HealingWaterCommand(HostileCharacter caster, ITargettable target, float healAmount,
                               GameObject projectilePrefab, TrajectoryConfigsSO trajectoryConfig)
    {
        _caster = caster;
        _target = target;
        _healAmount = healAmount;
        _projectilePrefab = projectilePrefab;
        _trajectoryConfig = trajectoryConfig;
    }

    public async Awaitable ExecuteAsync()
    {
        var t = _caster.Transform;
        var original = t.localScale;

        await Tween.Scale(t, original * 0.65f, 0.18f, Ease.InQuad);
        await Tween.Scale(t, original * 1.3f,  0.12f, Ease.OutQuad);
        await Tween.Scale(t, original,         0.12f, Ease.InOutQuad);

        await LaunchProjectile();
    }

    private async Awaitable LaunchProjectile()
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, _caster.Transform.position, Quaternion.identity);

        Vector3 start = _caster.Transform.position;
        Vector3 end   = _target.Transform.position;
        Vector3 ctrl  = (start + end) / 2f + Vector3.up * _trajectoryConfig.Height;

        var state = new ProjectileState(projectile.transform, start, ctrl, end);
        await Tween.Custom(state, 0f, 1f, duration: _trajectoryConfig.TravelDuration, ease: Ease.Linear,
            onValueChange: static (s, progress) =>
            {
                Vector3 cur = MathUtils.EvaluateBezierPoint(progress, s.Start, s.ControlPoint, s.End);
                s.Projectile.LookAt(MathUtils.EvaluateBezierPoint(progress + 0.01f, s.Start, s.ControlPoint, s.End));
                s.Projectile.position = cur;
            });

        if (_target is IHealthOwner healthOwner)
        {
            _hpBeforeHeal = healthOwner.Health.CurrentHp;
            healthOwner.Health.Heal(_healAmount);
        }

        GameObject.Destroy(projectile);
    }

    public void Undo()
    {
        if (_target is IHealthOwner healthOwner)
        {
            float delta = healthOwner.Health.CurrentHp - _hpBeforeHeal;
            if (delta > 0f)
                healthOwner.Health.TakeDamage(new DamagePayload(delta));
        }
    }

    private class ProjectileState
    {
        public readonly Transform Projectile;
        public readonly Vector3 Start, ControlPoint, End;

        public ProjectileState(Transform p, Vector3 s, Vector3 cp, Vector3 e)
        {
            Projectile   = p;
            Start        = s;
            ControlPoint = cp;
            End          = e;
        }
    }
}
