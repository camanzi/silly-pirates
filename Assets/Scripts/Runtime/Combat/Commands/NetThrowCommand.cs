using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class NetThrowCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly List<ITargettable> _targets;
    private readonly GameObject _projectilePrefab;
    private readonly int _cooldown;
    private readonly SlowPassiveSO _slowPassiveSO;
    private readonly TrajectoryConfigsSO _trajectoryConfigData;

    private readonly List<Awaitable> _flightTasks = new();

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public NetThrowCommand(
        IInteractableElement caster,
        List<ITargettable> targets,
        GameObject projectilePrefab,
        int cooldown,
        SlowPassiveSO slowPassiveSO,
        TrajectoryConfigsSO trajectoryConfigData)
    {
        _caster = caster;
        _targets = targets;
        _projectilePrefab = projectilePrefab;
        _cooldown = cooldown;
        _slowPassiveSO = slowPassiveSO;
        _trajectoryConfigData = trajectoryConfigData;
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
        var projectileComponent = projectile.GetComponent<Projectile>();

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

        HandleImpact(projectile, projectileComponent, target);
    }

    private void HandleImpact(GameObject projectile, Projectile projectileComponent, ITargettable target)
    {
        PassiveAbilityController passiveController = null;

        if (target is HostileCharacter hostile)
            passiveController = hostile.PassiveAbilityController;
        else if (target is GridCharacter gridCharacter)
            passiveController = gridCharacter.PassiveAbilityController;

        if (passiveController != null)
            passiveController.AddPassive(Object.Instantiate(_slowPassiveSO));

        projectileComponent?.PlayImpactEffect();
        GameObject.Destroy(projectile);
    }

    private async Awaitable ManualWaitForAll(List<Awaitable> awaitables)
    {
        for (int i = 0; i < awaitables.Count; i++)
            await awaitables[i];
    }

    public void Undo() { }
}
