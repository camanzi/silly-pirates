using UnityEngine;

/// <summary>
/// Richiesta di riprodurre un suono one-shot. Fire-and-forget: nessuno aspetta che finisca.
/// </summary>
public struct SfxCue
{
    public SoundEventSO Sound;

    /// <summary>Posizione nel mondo. Null = suono 2D non posizionale (UI).</summary>
    public Vector3? WorldPosition;

    /// <summary>Se valorizzato, la voce insegue questo Transform e ignora WorldPosition.</summary>
    public Transform FollowTarget;

    /// <summary>Moltiplicatore sul volume estratto dal SoundEventSO.</summary>
    public float VolumeScale;

    /// <summary>Moltiplicatore sul pitch estratto dal SoundEventSO.</summary>
    public float PitchScale;

    /// <summary>One-shot posizionale fisso: nessun costo per frame.</summary>
    public static SfxCue At(SoundEventSO sound, Vector3 worldPosition, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = worldPosition,
        FollowTarget = null,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };

    /// <summary>One-shot che insegue un bersaglio in movimento.</summary>
    public static SfxCue Follow(SoundEventSO sound, Transform target, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = null,
        FollowTarget = target,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };

    /// <summary>Suono 2D non posizionale (UI, stinger).</summary>
    public static SfxCue TwoD(SoundEventSO sound, float volumeScale = 1f) => new()
    {
        Sound = sound,
        WorldPosition = null,
        FollowTarget = null,
        VolumeScale = volumeScale,
        PitchScale = 1f
    };
}
