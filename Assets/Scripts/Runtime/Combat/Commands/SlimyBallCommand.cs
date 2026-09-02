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
    /// Lo slime salta fuori dall'acqua fino all'altezza del ponte (la Y del target), spara mentre è
    /// sospeso all'apice, e solo dopo ricade in acqua.
    /// </summary>
    public async Awaitable ExecuteAsync()
    {
        // I bersagli includono il corpo (indice 0) e i suoi satelliti: le parti di un nemico composito
        // devono saltare insieme a lui, non restare a terra.
        IReadOnlyList<LifecycleAnimationTarget> targets =
            _caster.LifecycleAnimator != null ? _caster.LifecycleAnimator.Targets : null;
        Transform bodyPivot = targets != null && targets.Count > 0 ? targets[0].Pivot : null;

        // Senza un pivot visivo dedicato (fallback su transform di griglia) o senza config non
        // animiamo il salto: sposteremmo collider e posizione di cella, o gireremmo con parametri
        // non pronti. Si degrada al vecchio squash-stretch sul root.
        bool canAnimateJump = bodyPivot != null && bodyPivot != _caster.Transform && _jumpConfig != null;

        if (!canAnimateJump)
        {
            await LegacySquashStretch();
            await LaunchProjectile(_caster.Transform.position);
            return;
        }

        // Letta prima di qualunque await: il calcolo dell'apice non può fallire più tardi.
        float targetWorldY = _target.Transform.position.y;
        float apexWorldHeight = _jumpConfig.ComputeApexHeight(_caster.Transform.position.y, targetWorldY);
        Vector3 splashWorldPosition = _caster.Transform.position + Vector3.up * _jumpConfig.SplashYOffset;
        CancellationToken token = _caster.destroyCancellationToken;

        // Il salto scrive localPosition E localScale del pivot: sono gli stessi due canali su cui gira
        // l'idle in loop. Senza sospenderlo si contenderebbero la Y del pivot ad ogni frame.
        _caster.LifecycleAnimator?.SuspendLoop();

        try
        {
            await JumpSquashStretchHelper.JumpUpAsync(
                targets, apexWorldHeight, _jumpConfig, splashWorldPosition, token);

            if (_jumpConfig.HangHoldDuration > 0f)
                await Tween.Delay(_jumpConfig.HangHoldDuration);

            // Il proiettile parte dalla posizione MONDO del pivot sollevato, non dal root a pelo d'acqua.
            await LegacySquashStretch();
            await LaunchProjectile(bodyPivot.position);

            await JumpSquashStretchHelper.FallDownAsync(
                targets, _jumpConfig, splashWorldPosition, token);
        }
        finally
        {
            // Ridondante col finally interno di FallDownAsync, ma necessario: se JumpUpAsync o
            // LaunchProjectile falliscono prima di arrivarci, i pivot non devono restare sospesi in aria.
            JumpSquashStretchHelper.ResetToRest(targets);

            _caster.LifecycleAnimator?.ResumeLoop();
        }
    }

    /// <summary>
    /// Comportamento originale, mantenuto come fallback quando non è disponibile un pivot visivo
    /// dedicato sicuro da animare.
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
