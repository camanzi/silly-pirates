using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SpawnAlliesCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly List<(HostileCharacter prefab, SpawnPoint point)> _spawnPairs;
    private readonly Transform _partTransform;
    private readonly VFXController _apexVfx;
    private readonly Vector3 _apexVfxWorldOffset;
    private readonly float _apexVfxScale;
    private readonly VfxCueEventChannel _vfxChannel;
    private readonly List<CharacterLifecycleAnimator> _spawnedAnimators = new();

    public SpawnAlliesCommand(
        HostileCharacter caster,
        List<(HostileCharacter prefab, SpawnPoint point)> spawnPairs,
        Transform partTransform = null,
        VFXController apexVfx = null,
        Vector3 apexVfxWorldOffset = default,
        float apexVfxScale = 1f,
        VfxCueEventChannel vfxChannel = null)
    {
        _caster = caster;
        _spawnPairs = spawnPairs;
        _partTransform = partTransform;
        _apexVfx = apexVfx;
        _apexVfxWorldOffset = apexVfxWorldOffset;
        _apexVfxScale = apexVfxScale;
        _vfxChannel = vfxChannel;
    }

    public async Awaitable ExecuteAsync()
    {
        // _partTransform is nullable by construction (EnemyAbilityBase.GetRequiredPartTransform uses ?.):
        // if the part is not registered or is broken, the spawn must happen anyway, without the sceptre's
        // animation.
        bool hasPart = _partTransform != null;
        Vector3 origin = hasPart ? _partTransform.position : Vector3.zero;

        if (hasPart)
        {
            // AnimatePart tweens part.position in WORLD coordinates, reading the current position as its
            // origin: the same channel that carries the caster's looping idle (localPosition on the same
            // pivot, when the part is the body or a registered satellite).
            _caster.LifecycleAnimator?.SuspendLoop();
        }

        try
        {
            if (hasPart)
                await AnimatePart(_partTransform);

            _spawnedAnimators.Clear();
            foreach (var (prefab, spawnPoint) in _spawnPairs)
            {
                var spawnedGO = UnityEngine.Object.Instantiate(prefab.gameObject, spawnPoint.Position, Quaternion.identity);
                var spawned = spawnedGO.GetComponent<HostileCharacter>();

                spawnPoint.Claim(spawned);

                if (spawned.LifecycleAnimator != null)
                    _spawnedAnimators.Add(spawned.LifecycleAnimator);
            }

            // The token is the spawned one's, not the caster's: it is its animation that is being awaited,
            // and its destruction is what has to unblock the wait.
            foreach (var animator in _spawnedAnimators)
            {
                try
                {
                    await animator.WaitUntilIdleAsync(animator.destroyCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // The spawned one was destroyed while emerging: move on to the next without aborting
                    // the sceptre's return.
                }
            }

            if (hasPart)
                await Tween.Position(_partTransform, origin, 0.4f, Ease.InQuad);
        }
        finally
        {
            // In the finally and not after the sceptre comes back down: if the spawn blows up halfway, the
            // caster's loop has to start running again regardless. StopLoop()'s safety net only covers
            // whoever changes phase, and this ability's caster stays alive and idle.
            if (hasPart) _caster.LifecycleAnimator?.ResumeLoop();
        }

        await Awaitable.WaitForSecondsAsync(0.3f);
    }

    // Sequence:
    // 1. Move part up 1 unit
    // 2. Hold 0.25s
    // 3. Orbit 2 full circles around local pivot with radius 0.75f (on XZ plane)
    // 4. Move up additional 0.5 units
    // 5. Return to original position
    private async Awaitable AnimatePart(Transform part)
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

        if (_apexVfx != null && _vfxChannel != null)
            _vfxChannel.RaiseEvent(VfxCue.At(_apexVfx, part.position + _apexVfxWorldOffset, _apexVfxScale));

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
