/// <summary>
/// Richiesta al MusicDirector: riproduci questa traccia (o ferma quella corrente).
/// Non porta un handle: esiste una sola traccia musicale corrente, e' il MusicDirector a
/// possederne l'handle e a gestire il crossfade con quella uscente.
/// </summary>
public struct MusicCue
{
    /// <summary>Traccia da avviare. Null = ferma soltanto la traccia corrente.</summary>
    public SoundEventSO Track;

    public float FadeInSeconds;

    /// <summary>Fade-out della traccia USCENTE (quella attualmente in riproduzione, se presente).</summary>
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
