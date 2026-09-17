using UnityEngine;

/// <summary>
/// A request to play a one-shot sound. Fire-and-forget: nobody waits for it to end.
/// </summary>
public struct SfxCue
{
    public SoundEventSO Sound;

    /// <summary>Position in the world. Null = a non-positional 2D sound (UI).</summary>
    public Vector3? WorldPosition;

    /// <summary>When set, the voice follows this Transform and ignores WorldPosition.</summary>
    public Transform FollowTarget;

    /// <summary>Multiplier on the volume drawn from the SoundEventSO.</summary>
    public float VolumeScale;

    /// <summary>Multiplier on the pitch drawn from the SoundEventSO.</summary>
    public float PitchScale;

    /// <summary>A one-shot at a fixed position: no per-frame cost.</summary>
    public static SfxCue At(SoundEventSO sound, Vector3 worldPosition, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = worldPosition,
        FollowTarget = null,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };

    /// <summary>A one-shot that follows a moving target.</summary>
    public static SfxCue Follow(SoundEventSO sound, Transform target, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = null,
        FollowTarget = target,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };

    /// <summary>A non-positional 2D sound (UI, stingers).</summary>
    public static SfxCue TwoD(SoundEventSO sound, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = null,
        FollowTarget = null,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };
}
