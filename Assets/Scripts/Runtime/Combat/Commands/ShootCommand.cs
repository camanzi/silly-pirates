using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class ShootCommand : ICommand
{
    private IInteractableElement _caster;
    private List<ITargettable> _targets;
    private DamageTypeProjectileConfigSO _projectileConfig;
    private int _cooldown;

    private TrajectoryConfigsSO _trajectoryConfigData;
    private readonly int _baseDMG;
    private readonly DamageType _baseDMGType;
    private readonly IOffensiveEquipmentStats _stats;

    private readonly int _overcapCritBonus;

    private readonly List<Awaitable> _flightTasks = new();

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public ShootCommand(IInteractableElement caster, List<ITargettable> targets, DamageTypeProjectileConfigSO projectileConfig, int cooldown, int baseDMG, DamageType baseDMGType, IOffensiveEquipmentStats stats, TrajectoryConfigsSO trajectoryConfigData, int overcapCritBonus = 0)
    {
        _caster = caster;
        _targets = targets;
        _projectileConfig = projectileConfig;
        _cooldown = cooldown;
        _baseDMG = baseDMG;
        _baseDMGType = baseDMGType;
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

    private DamageType ResolveDMGType()
    {
        if (_caster is IDMGTypeOwner dmgOwner)
        {
            DamageType overridden = dmgOwner.EffectiveDMGType;
            if (overridden != DamageType.None) return overridden;
        }
        return _baseDMGType;
    }

    private async Awaitable LaunchProjectile(ITargettable target)
    {
        DamageType effectiveType = ResolveDMGType();
        var projectile = GameObject.Instantiate(_projectileConfig.GetPrefab(effectiveType), _caster.Transform.position, Quaternion.identity);

        Vector3 start = _caster.Transform.position;
        Vector3 end = target.Transform.position;
        Vector3 controlPoint = (start + end) / 2 + Vector3.up * _trajectoryConfigData.Height;

        if (_caster is ShipEquipment equipment) equipment.OnCommandExecuted?.Invoke();

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
        if (target is IHealthOwner healthOwner)
        {
            float damage   = _baseDMG;
            int   critRate = (_stats?.CritRate ?? 0) + _overcapCritBonus;

            int effectiveCritDMG = _caster is ICritDMGOwner critOwner ? critOwner.EffectiveCritDMG : (_stats?.CritDMG ?? 0);
            bool isCrit = UnityEngine.Random.Range(0, 100) < critRate;
            if (isCrit)
                damage += damage * effectiveCritDMG / 100f;

            DamageType dmgType = ResolveDMGType();
            healthOwner.Health.TakeDamage(new DamagePayload(damage, dmgType) { IsCritical = isCrit });
            Debug.Log($"[ShootCommand] Hit — crit: {isCrit}, damage: {damage}, dmgType: {dmgType}, critRate: {critRate}%, critDMG: {effectiveCritDMG}%");
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
