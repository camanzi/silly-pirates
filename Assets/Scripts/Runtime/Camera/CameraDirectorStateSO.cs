using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraDirectorState", menuName = "Camera/Camera Director State")]
public class CameraDirectorStateSO : ScriptableObject, ICombatSessionResettable
{
    [Tooltip("Safety cap: command execution never waits longer than this for the camera to get in position")]
    [Min(0.5f)]
    [SerializeField] private float _maxWaitSeconds = 5f;

    public bool IsFocused { get; private set; } = true;
    public float MaxWaitSeconds => _maxWaitSeconds;

    public event Action OnFocusEnded;

    public void BeginFocus() => IsFocused = false;

    public void SignalFocusReady() => IsFocused = true;

    public void EndFocus()
    {
        IsFocused = true;
        OnFocusEnded?.Invoke();
    }

    public async Awaitable WaitUntilFocused()
    {
        float deadline = Time.time + _maxWaitSeconds;
        while (!IsFocused && Time.time < deadline)
            await Awaitable.NextFrameAsync();

        // Never leave the flag stale if no CameraDirector answered the cue
        IsFocused = true;
    }

    /// <summary>
    /// Raises a cue and waits for the camera to settle, for callers (e.g. a command mid-execution)
    /// that need to trigger additional camera beats beyond the turn-start cue.
    /// </summary>
    public async Awaitable RaiseCueAndWaitAsync(AbilityExecutionCueEventChannel channel, AbilityExecutionCue cue)
    {
        if (channel == null) return;
        BeginFocus();
        channel.RaiseEvent(cue);
        await WaitUntilFocused();
    }

    private void OnEnable() => IsFocused = true;

    // Se un cue restasse a metà (comando interrotto dallo scarico scena), IsFocused resterebbe
    // false per sempre e il prossimo WaitUntilFocused del nuovo combattimento si bloccherebbe fino
    // al timeout di sicurezza invece di partire libero.
    public void ResetForNewCombat() => IsFocused = true;
}
