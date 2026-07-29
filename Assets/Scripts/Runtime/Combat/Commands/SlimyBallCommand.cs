using PrimeTween;
using UnityEngine;

public class SlimyBallCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly ITargettable _target;
    private readonly GameObject _projectilePrefab;
    private readonly TrajectoryConfigsSO _trajectoryConfig;
    private readonly int _damage;
    private readonly DamageType _damageType;
    private readonly SlimyCellDataSO _slimyCellData;
    private readonly Vector3Int _targetCell;
    private readonly EnemyCritStatsSO _critStats;

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public SlimyBallCommand(IInteractableElement caster, ITargettable target,
                             GameObject projectilePrefab, TrajectoryConfigsSO trajectoryConfig, int damage,
                             DamageType damageType = DamageType.Physical,
                             SlimyCellDataSO slimyCellData = null, Vector3Int targetCell = default,
                             EnemyCritStatsSO critStats = null)
    {
        _caster = caster;
        _target = target;
        _projectilePrefab = projectilePrefab;
        _trajectoryConfig = trajectoryConfig;
        _damage = damage;
        _damageType = damageType;
        _slimyCellData = slimyCellData;
        _targetCell = targetCell;
        _critStats = critStats;
    }

    public async Awaitable ExecuteAsync()
    {
        var t = _caster.Transform;
        var original = t.localScale;

        await Tween.Scale(t, original * 0.6f, 0.15f, Ease.InQuad);
        await Tween.Scale(t, original * 1.2f, 0.10f, Ease.OutQuad);
        await Tween.Scale(t, original,        0.10f, Ease.InOutQuad);

        await LaunchProjectile();
    }

    private async Awaitable LaunchProjectile()
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, _caster.Transform.position, Quaternion.identity);
        var projectileComponent = projectile.GetComponent<Projectile>();

        Vector3 start = _caster.Transform.position;
        Vector3 end   = _target.Transform.position;
        Vector3 ctrl  = (start + end) / 2f + Vector3.up * _trajectoryConfig.Height;

        var state = new ProjectileState(projectile.transform, start, ctrl, end);
        await Tween.Custom(state, 0f, 1f, duration: _trajectoryConfig.TravelDuration, ease: Ease.Linear,
            onValueChange: static (s, progress) =>
            {
                Vector3 cur = MathUtils.EvaluateBezierPoint(progress, s.Start, s.ControlPoint, s.End);
                s.Projectile.LookAt(MathUtils.EvaluateBezierPoint(progress + 0.01f, s.Start, s.ControlPoint, s.End));
                s.Projectile.position   = cur;
                s.Projectile.localScale = ProjectileScale;
            });

        if (_target is IHealthOwner healthOwner)
        {
            float damage = _damage;
            bool  isCrit = false;
            if (_critStats != null)
            {
                isCrit = UnityEngine.Random.Range(0, 100) < _critStats.CritRate;
                if (isCrit) damage += damage * _critStats.CritDMG / 100f;
            }
            healthOwner.Health.TakeDamage(new DamagePayload(damage, _damageType) { IsCritical = isCrit });
        }

        _slimyCellData?.Apply(_targetCell);

        projectileComponent?.PlayImpactEffect();
        GameObject.Destroy(projectile);
    }

    public void Undo() { }

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
