using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// The Broadside as a single command: every shot starts after its own delay and they all fly together.
/// Each shot is started without being awaited and its Awaitable kept, then all of them are awaited in turn
/// — the same scheme ShootCommand uses for the projectiles of a multishot. ShootCommand/NetThrowCommand
/// keep their state per instance, so running several at once is safe.
/// </summary>
public class BroadsideCommand : ICommand
{
    public readonly struct Shot
    {
        public readonly ICommand Command;
        public readonly float StartDelay;

        public Shot(ICommand command, float startDelay)
        {
            Command = command;
            StartDelay = startDelay;
        }
    }

    private readonly List<Shot> _shots;
    private readonly CancellationToken _token;
    private readonly List<Awaitable> _running = new();

    public BroadsideCommand(List<Shot> shots, CancellationToken token)
    {
        _shots = shots ?? new List<Shot>();
        _token = token;
    }

    public int ShotCount => _shots.Count;

    public async Awaitable ExecuteAsync()
    {
        _running.Clear();
        for (int i = 0; i < _shots.Count; i++)
            _running.Add(RunShotAsync(_shots[i]));

        for (int i = 0; i < _running.Count; i++)
            await _running[i];
    }

    private async Awaitable RunShotAsync(Shot shot)
    {
        if (shot.StartDelay > 0f)
            await Awaitable.WaitForSecondsAsync(shot.StartDelay, _token);

        if (shot.Command != null)
            await shot.Command.ExecuteAsync();
    }

    public void Undo()
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
            _shots[i].Command?.Undo();
    }
}
