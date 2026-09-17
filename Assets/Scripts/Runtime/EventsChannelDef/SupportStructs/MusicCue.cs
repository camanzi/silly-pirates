/// <summary>
/// A request to the MusicDirector: play this track (or stop the current one).
/// It carries no handle: there is only ever one current music track, and it is the MusicDirector that
/// owns its handle and handles the crossfade with the outgoing one.
/// </summary>
public struct MusicCue
{
    /// <summary>The track to start. Null = merely stop the current track.</summary>
    public SoundEventSO Track;

    public float FadeInSeconds;

    /// <summary>Fade-out of the OUTGOING track (the one currently playing, if any).</summary>
    public float FadeOutSeconds;

    public static MusicCue Play(SoundEventSO track, float fadeInSeconds = 0f, float fadeOutSeconds = 0f) => new()
    {
        Track = track,
        FadeInSeconds = fadeInSeconds,
        FadeOutSeconds = fadeOutSeconds
    };

    public static MusicCue Stop(float fadeOutSeconds = 0f) => new()
    {
        Track = null,
        FadeInSeconds = 0f,
        FadeOutSeconds = fadeOutSeconds
    };
}
