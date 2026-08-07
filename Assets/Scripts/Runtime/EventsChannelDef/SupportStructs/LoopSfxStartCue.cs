using UnityEngine;

/// <summary>
/// Avvia un suono in loop. L'handle va coniato dal chiamante con <see cref="AudioLoopHandle.New"/>
/// e conservato: e' l'unico modo per fermare il loop.
/// </summary>
public struct LoopSfxStartCue
{
    public AudioLoopHandle Handle;
    public SoundEventSO Sound;

    /// <summary>Posizione nel mondo. Null = suono 2D non posizionale.</summary>
    public Vector3? WorldPosition;

    /// <summary>Se valorizzato, la voce insegue questo Transform e ignora WorldPosition.</summary>
    public Transform FollowTarget;

    public float VolumeScale;
    public float FadeInSeconds;

    public static LoopSfxStartCue At(AudioLoopHandle handle, SoundEventSO sound, Vector3 worldPosition,
        float volumeScale = 1f, float fadeInSeconds = 0f) => new()
    {
        Handle = handle,
        Sound = sound,
        WorldPosition = worldPosition,
        FollowTarget = null,
        VolumeScale = volumeScale,
        FadeInSeconds = fadeInSeconds
    };

    public static LoopSfxStartCue Follow(AudioLoopHandle handle, SoundEventSO sound, Transform target,
        float volumeScale = 1f, float fadeInSeconds = 0f) => new()
    {
        Handle = handle,
        Sound = sound,
        WorldPosition = null,
        FollowTarget = target,
        VolumeScale = volumeScale,
        FadeInSeconds = fadeInSeconds
    };

    public static LoopSfxStartCue TwoD(AudioLoopHandle handle, SoundEventSO sound,
        float volumeScale = 1f, float fadeInSeconds = 0f) => new()
    {
        Handle = handle,
        Sound = sound,
        WorldPosition = null,
        FollowTarget = null,
        VolumeScale = volumeScale,
        FadeInSeconds = fadeInSeconds
    };
}
