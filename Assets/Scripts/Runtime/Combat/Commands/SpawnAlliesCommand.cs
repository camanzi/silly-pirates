using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SpawnAlliesCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly List<(HostileCharacter prefab, SpawnPoint point)> _spawnPairs;
    private readonly Transform _partTransform;
    private readonly List<CharacterLifecycleAnimator> _spawnedAnimators = new();

    public SpawnAlliesCommand(
        HostileCharacter caster,
        List<(HostileCharacter prefab, SpawnPoint point)> spawnPairs,
        Transform partTransform = null)
    {
        _caster = caster;
        _spawnPairs = spawnPairs;
        _partTransform = partTransform;
    }

    public async Awaitable ExecuteAsync()
    {
        // _partTransform è nullable per costruzione (EnemyAbilityBase.GetRequiredPartTransform usa ?.):
        // se la parte non è registrata o è rotta, lo spawn deve avvenire comunque, senza l'animazione
        // dello scettro.
        bool hasPart = _partTransform != null;
        Vector3 origin = hasPart ? _partTransform.position : Vector3.zero;

        if (hasPart)
            await AnimatePart(_partTransform);

        _spawnedAnimators.Clear();
        foreach (var (prefab, spawnPoint) in _spawnPairs)
        {
            var spawnedGO = Object.Instantiate(prefab.gameObject, spawnPoint.Position, Quaternion.identity);
            var spawned = spawnedGO.GetComponent<HostileCharacter>();

            spawnPoint.Claim(spawned);

            if (spawned.LifecycleAnimator != null)
                _spawnedAnimators.Add(spawned.LifecycleAnimator);
        }

        foreach (var animator in _spawnedAnimators)
            await animator.WaitUntilIdleAsync(_caster.destroyCancellationToken);

        if (hasPart)
            await Tween.Position(_partTransform, origin, 0.4f, Ease.InQuad);

        await Awaitable.WaitForSecondsAsync(0.3f);
    }

    // Sequence:
    // 1. Move part up 1 unit
    // 2. Hold 0.25s
    // 3. Orbit 2 full circles around local pivot with radius 0.75f (on XZ plane)
    // 4. Move up additional 0.5 units
    // 5. Return to original position
    private static async Awaitable AnimatePart(Transform part)
    {
        Vector3 origin = part.position;

        await Tween.Position(part, origin + Vector3.up, 0.3f, Ease.OutQuad);

        await Awaitable.WaitForSecondsAsync(0.75f);

        Vector3 orbitCenter = part.position;
        const float radius = 0.75f;
        const float orbitDuration = 1.0f;

        var orbitState = new OrbitState(part, orbitCenter, radius);
        await Tween.Custom(orbitState, 0f, 1f, duration: orbitDuration, ease: Ease.Linear,
            onValueChange: static (s, t) =>
            {
                float angle = t * 2f * Mathf.PI * 2f; // 2 full rotations = 4π
                float x = s.Center.x + Mathf.Sin(angle) * s.Radius;
                float z = s.Center.z + Mathf.Cos(angle) * s.Radius;
                s.Part.position = new Vector3(x, s.Center.y, z);
            });

        part.position = orbitCenter;

        await Tween.Position(part, orbitCenter + Vector3.up * 1f, .75f, Ease.OutQuad);

        await Awaitable.WaitForSecondsAsync(1f);
    }

    public void Undo() { }

    private class OrbitState
    {
        public readonly Transform Part;
        public readonly Vector3 Center;
        public readonly float Radius;

        public OrbitState(Transform part, Vector3 center, float radius)
        {
            Part   = part;
            Center = center;
            Radius = radius;
        }
    }
}
