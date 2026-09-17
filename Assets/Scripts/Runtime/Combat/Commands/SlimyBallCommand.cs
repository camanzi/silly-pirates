using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class SlimyBallCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly ITargettable _target;
    private readonly GameObject _projectilePrefab;
    private readonly TrajectoryConfigsSO _trajectoryConfig;
    private readonly int _damage;
    private readonly DamageType _damageType;
    private readonly SlimyCellDataSO _slimyCellData;
    private readonly Vector3Int _targetCell;
    private readonly EnemyCritStatsSO _critStats;
    private readonly JumpAnimationConfigSO _jumpConfig;

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public SlimyBallCommand(HostileCharacter caster, ITargettable target,
                             GameObject projectilePrefab, TrajectoryConfigsSO trajectoryConfig, int damage,
                             DamageType damageType = DamageType.Physical,
                             SlimyCellDataSO slimyCellData = null, Vector3Int targetCell = default,
                             EnemyCritStatsSO critStats = null,
                             JumpAnimationConfigSO jumpConfig = null)
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
        _jumpConfig = jumpConfig;
    }

    /// <summary>
    /// The slime leaps out of the water up to deck height (the target's Y), fires while it hangs at the
    /// apex, and only then falls back into the water.
    /// </summary>
    public async Awaitable ExecuteAsync()
    {
        // The targets include the body (index 0) and its satellites: the parts of a composite enemy have
        // to jump along with it rather than stay on the ground.
        IReadOnlyList<LifecycleAnimationTarget> targets =
            _caster.LifecycleAnimator != null ? _caster.LifecycleAnimator.Targets : null;
        Transform bodyPivot = targets != null && targets.Count > 0 ? targets[0].Pivot : null;

        // With no dedicated visual pivot (it falls back to the grid transform) or no config, the jump is
        // not animated: it would move the collider and the cell position, or run with parameters that are
        // not ready. It degrades to the old squash-stretch on the root.
        bool canAnimateJump = bodyPivot != null && bodyPivot != _caster.Transform && _jumpConfig != null;

        if (!canAnimateJump)
        {
            await LegacySquashStretch();
            await LaunchProjectile(_caster.Transform.position);
            return;
        }

        // Read before any await: the apex computation must not be able to fail later on.
        float targetWorldY = _target.Transform.position.y;
        float apexWorldHeight = _jumpConfig.ComputeApexHeight(_caster.Transform.position.y, targetWorldY);
        Vector3 splashWorldPosition = _caster.Transform.position + Vector3.up * _jumpConfig.SplashYOffset;
        CancellationToken token = _caster.destroyCancellationToken;

        // The jump writes the pivot's localPosition AND localScale: the same two channels that carry the
        // looping idle. Without suspending it, the two would fight over the pivot's Y every frame.
        _caster.LifecycleAnimator?.SuspendLoop();

        try
        {
            await JumpSquashStretchHelper.JumpUpAsync(
                targets, apexWorldHeight, _jumpConfig, splashWorldPosition, token);

            if (_jumpConfig.HangHoldDuration > 0f)
                await Tween.Delay(_jumpConfig.HangHoldDuration);

            // The projectile leaves from the WORLD position of the raised pivot, not from the root at
            // water level.
            await LegacySquashStretch();
            await LaunchProjectile(bodyPivot.position);

            await JumpSquashStretchHelper.FallDownAsync(
                targets, _jumpConfig, splashWorldPosition, token);
        }
        finally
        {
            // Redundant with FallDownAsync's own finally, but necessary: if JumpUpAsync or
            // LaunchProjectile fail before reaching it, the pivots must not be left hanging in mid-air.
            JumpSquashStretchHelper.ResetToRest(targets);

            _caster.LifecycleAnimator?.ResumeLoop();
        }
    }

    /// <summary>
    /// The original behaviour, kept as a fallback for when there is no dedicated visual pivot that is
    /// safe to animate.
    /// </summary>
    private async Awaitable LegacySquashStretch()
    {
        var t = _caster.Transform;
        var original = t.localScale;

        await Tween.Scale(t, original * 0.6f, 0.15f, Ease.InQuad);
        await Tween.Scale(t, original * 1.2f, 0.10f, Ease.OutQuad);
        await Tween.Scale(t, original,        0.10f, Ease.InOutQuad);
    }

    private async Awaitable LaunchProjectile(Vector3 startPosition)
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, startPosition, Quaternion.identity);
        var projectileComponent = projectile.GetComponent<Projectile>();

        Vector3 start = startPosition;
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
