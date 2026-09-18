using UnityEngine;

/// <summary>
/// Translates <see cref="PauseStateSO"/> into the only thing that actually freezes the game:
/// Time.timeScale. This component is the sole owner of that property in the whole project — anything
/// else wanting to stop the world raises the pause state instead of writing the timescale itself.
/// </summary>
public class PauseTimeController : MonoBehaviour
{
    [SerializeField] private PauseStateSO _pauseState;

    private void OnEnable()
    {
        if (_pauseState == null) return;

        // Pull-then-subscribe: were the pause already active by the time this object enables,
        // subscribing alone would miss the event and the game would keep running at full speed.
        ApplyTimeScale(_pauseState.IsPaused);
        _pauseState.OnPauseChanged += ApplyTimeScale;
    }

    private void OnDisable()
    {
        if (_pauseState != null) _pauseState.OnPauseChanged -= ApplyTimeScale;

        // The safety net, and it is not optional: Time.timeScale is global engine state that survives
        // this object. Quitting to the menu from the pause menu destroys this component while the game
        // is frozen, and without this line the next scene would stay frozen forever with nothing left
        // listening to thaw it.
        Time.timeScale = 1f;
    }

    private void ApplyTimeScale(bool paused) => Time.timeScale = paused ? 0f : 1f;
}
