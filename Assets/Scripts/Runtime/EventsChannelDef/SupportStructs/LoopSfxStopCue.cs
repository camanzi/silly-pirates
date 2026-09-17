/// <summary>
/// Stops a looping sound. An unknown handle (a loop already stopped, or never started) is a silent
/// no-op: a double stop is a normal case, not an error.
/// </summary>
public struct LoopSfxStopCue
{
    public AudioLoopHandle Handle;
    public float FadeOutSeconds;

    public static LoopSfxStopCue Of(AudioLoopHandle handle, float fadeOutSeconds = 0f) => new()
    {
        Handle = handle,
        FadeOutSeconds = fadeOutSeconds
    };
}
