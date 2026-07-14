using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraDirectorState", menuName = "Camera/Camera Director State")]
public class CameraDirectorStateSO : ScriptableObject
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

    private void OnEnable() => IsFocused = true;
}
