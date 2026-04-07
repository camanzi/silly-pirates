using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class ShootCommand : ICommand
{
    private IInteractableElement _caster;
    private List<ITargettable> _targets;
    private GameObject _projectilePrefab;

    private TrajectoryConfigsSO _trajectoryConfigData;
    private float _damage = 10f;

    public ShootCommand(IInteractableElement caster, List<ITargettable> targets, GameObject prefab, TrajectoryConfigsSO trajectoryConfigData)
    {
        _caster = caster;
        _targets = targets;
        _projectilePrefab = prefab;

        _trajectoryConfigData = trajectoryConfigData;
    }

    public async Awaitable ExecuteAsync()
    {
        List<Awaitable> flightTasks = new List<Awaitable>();

        for (int i = 0; i < _targets.Count; i++)
        {
            var target = _targets[i];

            await Tween.Rotation(_caster.Transform, 
                Quaternion.LookRotation(target.Transform.position - _caster.Transform.position), 
                duration: 0.2f);

            flightTasks.Add(LaunchProjectile(target));

            if (i < _targets.Count - 1)
                await Awaitable.WaitForSecondsAsync(0.3f); 
        }

        await ManualWaitForAll(flightTasks);
    }

    private async Awaitable LaunchProjectile(ITargettable target)
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, _caster.Transform.position, Quaternion.identity);
        
        Vector3 start = _caster.Transform.position;
        Vector3 end = target.Transform.position;
        Vector3 controlPoint = ((start + end) / 2) + Vector3.up * _trajectoryConfigData.Height;

        // FIXME Da implementare shake sulla virtual camera stessa usando impulse
        // _ = Tween.ShakeLocalPosition(Camera.main.transform, strength: new Vector3(1f, 1f, 0), duration: 0.4f);

        await Tween.Custom(0f, 1f, duration: _trajectoryConfigData.TravelDuration, ease: Ease.Linear, onValueChange: t => 
        {
            Vector3 currentPos = MathUtils.EvaluateBezierPoint(t, start, controlPoint, end);
            
            projectile.transform.LookAt(MathUtils.EvaluateBezierPoint(t + 0.01f, start, controlPoint, end));
            projectile.transform.position = currentPos;

            projectile.transform.localScale = new Vector3(0.8f, 0.8f, 1.4f); 
        });

        HandleImpact(projectile, target);
    }

    private void HandleImpact(GameObject projectile, ITargettable target)
    {
        Debug.Log($"[LOGIC] {target.Transform.name} ha ricevuto {_damage} danni.");
        
        GameObject.Destroy(projectile);
    }

    private async Awaitable ManualWaitForAll(List<Awaitable> awaitables)
    {
        foreach (var task in awaitables)
        {
            await task;
        }
    }

    public void Undo() { }
}
