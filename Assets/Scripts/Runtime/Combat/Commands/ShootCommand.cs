using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class ShootCommand : ICommand
{
    private IInteractableElement _caster;
    private List<ITargettable> _targets;
    private GameObject _projectilePrefab;
    private int _cooldown;

    private TrajectoryConfigsSO _trajectoryConfigData;
    private IOffensiveEquipmentStats _stats;

    private readonly int _overcapCritBonus;

    private readonly List<Awaitable> _flightTasks = new();

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public ShootCommand(IInteractableElement caster, List<ITargettable> targets, GameObject prefab, int cooldown, IOffensiveEquipmentStats stats, TrajectoryConfigsSO trajectoryConfigData, int overcapCritBonus = 0)
    {
        _caster = caster;
        _targets = targets;
        _projectilePrefab = prefab;
        _cooldown = cooldown;
        _stats = stats;
        _trajectoryConfigData = trajectoryConfigData;
        _overcapCritBonus = overcapCritBonus;
    }

    public async Awaitable ExecuteAsync()
    {
        if (_caster is IAwakable awakable)
            awakable.ConsumeAllAwakeningPoints();

        if (_caster is IAwakable awakableElement) awakableElement.Cooldown = _cooldown;

        _flightTasks.Clear();

        for (int i = 0; i < _targets.Count; i++)
        {
            var target = _targets[i];

            await Tween.Rotation(_caster.Transform,
                Quaternion.LookRotation(target.Transform.position - _caster.Transform.position),
                duration: 0.2f);

            _flightTasks.Add(LaunchProjectile(target));

            if (i < _targets.Count - 1)
                await Awaitable.WaitForSecondsAsync(0.3f);
        }

        await ManualWaitForAll(_flightTasks);
    }

    private class ProjectileState
    {
        public Transform Projectile;
        public Vector3 Start, ControlPoint, End;
        public ProjectileState(Transform p, Vector3 s, Vector3 cp, Vector3 e)
        { Projectile = p; Start = s; ControlPoint = cp; End = e; }
    }

    private async Awaitable LaunchProjectile(ITargettable target)
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, _caster.Transform.position, Quaternion.identity);

        Vector3 start = _caster.Transform.position;
        Vector3 end = target.Transform.position;
        Vector3 controlPoint = (start + end) / 2 + Vector3.up * _trajectoryConfigData.Height;

        if (_caster is ShootingEquipment shootingEquipment) shootingEquipment.OnShootEffects?.Invoke();

        var state = new ProjectileState(projectile.transform, start, controlPoint, end);
        await Tween.Custom(state, 0f, 1f, duration: _trajectoryConfigData.TravelDuration, ease: Ease.Linear,
            onValueChange: static (s, t) =>
            {
                Vector3 cur = MathUtils.EvaluateBezierPoint(t, s.Start, s.ControlPoint, s.End);
                s.Projectile.LookAt(MathUtils.EvaluateBezierPoint(t + 0.01f, s.Start, s.ControlPoint, s.End));
                s.Projectile.position = cur;
                s.Projectile.localScale = ProjectileScale;
            });

        HandleImpact(projectile, target);
    }

    private void HandleImpact(GameObject projectile, ITargettable target)
    {
        if (target is IHealthOwner healthOwner && _stats != null)
        {
            float damage   = _stats.BaseDMG;
            int   critRate = _stats.CritRate + _overcapCritBonus;

            bool isCrit = UnityEngine.Random.Range(0, 100) < critRate;
            if (isCrit)
                damage += damage * _stats.CritDMG / 100f;

            Debug.Log($"[ShootCommand] Hit — crit: {isCrit}, damage: {damage}, critRate: {critRate}%, critDMG: {_stats.CritDMG}%");
            healthOwner.Health.TakeDamage(new DamagePayload(damage, _stats.DMGType));
        }
        GameObject.Destroy(projectile);
    }

    private async Awaitable ManualWaitForAll(List<Awaitable> awaitables)
    {
        for (int i = 0; i < awaitables.Count; i++)
            await awaitables[i];
    }

    public void Undo() { }
}
