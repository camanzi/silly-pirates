using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SpawnAlliesCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly List<(HostileCharacter prefab, SpawnPoint point)> _spawnPairs;

    public SpawnAlliesCommand(
        HostileCharacter caster,
        List<(HostileCharacter prefab, SpawnPoint point)> spawnPairs)
    {
        _caster = caster;
        _spawnPairs = spawnPairs;
    }

    public async Awaitable ExecuteAsync()
    {
        var t = _caster.Transform;
        Vector3 origin = t.position;
        const float bounceHeight = 1f;

        for (int i = 0; i < 2; i++)
        {
            await Tween.Position(t, origin + Vector3.up * bounceHeight, 0.12f, Ease.OutQuad);
            await Tween.Position(t, origin, 0.12f, Ease.InQuad);
        }

        foreach (var (prefab, spawnPoint) in _spawnPairs)
        {
            var spawnedGO = Object.Instantiate(prefab.gameObject, spawnPoint.Position, Quaternion.identity);
            var spawned = spawnedGO.GetComponent<HostileCharacter>();

            spawnPoint.Claim(spawned);

            await Awaitable.WaitForSecondsAsync(0.15f);
        }

        await Awaitable.WaitForSecondsAsync(0.3f);
    }

    public void Undo() { }
}
