using UnityEngine;

/// <summary>
/// Starts a looping sound. The handle has to be minted by the caller with
/// <see cref="AudioLoopHandle.New"/> and kept: it is the only way to stop the loop.
/// </summary>
public struct LoopSfxStartCue
{
    public AudioLoopHandle Handle;
    public SoundEventSO Sound;

    /// <summary>Position in the world. Null = a non-positional 2D sound.</summary>
    public Vector3? WorldPosition;

    /// <summary>When set, the voice follows this Transform and ignores WorldPosition.</summary>
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
