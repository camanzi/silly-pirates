/// <summary>
/// Ferma un suono in loop. Un handle sconosciuto (loop gia' fermato, o mai avviato)
/// e' un no-op silenzioso: il doppio stop e' un caso normale, non un errore.
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
