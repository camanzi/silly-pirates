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

    /// <summary>
    /// True from the first <see cref="BeginFocus"/> until <see cref="EndFocus"/>: the whole window in which
    /// the director owns the screen, not just the part where the shot is still being composed. Whoever has to
    /// stay out of the way while a cue plays (the free-roam camera target) reads this.
    ///
    /// A bool and not a counter, because cues nest: <see cref="RaiseCueAndWaitAsync"/> begins a focus without
    /// ever ending it (the intermediate beats of a multi-impact command, the intro's per-enemy beats), and the
    /// single closing EndFocus comes from the owner's finally. A counter would drift out of sync; this cannot.
    /// </summary>
    public bool IsCinematicActive { get; private set; }

    public event Action OnFocusEnded;

    public void BeginFocus()
    {
        IsFocused = false;
        IsCinematicActive = true;
    }

    public void SignalFocusReady() => IsFocused = true;

    public void EndFocus()
    {
        IsFocused = true;
        IsCinematicActive = false;
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

    private void OnEnable()
    {
        IsFocused = true;
        IsCinematicActive = false;
    }

    // Were a cue to be left halfway (a command interrupted by a scene unload), IsFocused would stay false
    // forever and the new combat's next WaitUntilFocused would block until the safety timeout instead of
    // starting free.
    public void ResetForNewCombat()
    {
        IsFocused = true;
        IsCinematicActive = false;
    }
}
