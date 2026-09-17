using UnityEngine;

/// <summary>
/// An audio voice that can be borrowed from the pool. It wraps an AudioSource and carries the domain
/// state (originating sound, target to follow, loop) the generic pool has no interest in.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioVoice : PooledBehaviour
{
    private AudioSource _source;

    public AudioSource Source => EnsureSource();

    /// <summary>The SoundEventSO that spawned this voice, or null when it is free.</summary>
    public SoundEventSO Sound { get; private set; }

    /// <summary>When set, the AudioDirector copies the target's position every LateUpdate.</summary>
    public Transform FollowTarget { get; set; }

    public bool IsLooping { get; private set; }

    /// <summary>The handle of the loop currently playing, or None. It is what lets the map of active
    /// loops be cleaned up even when the voice is released through a path other than an explicit stop.</summary>
    public AudioLoopHandle LoopHandle { get; set; }

    /// <summary>Incremented on every Play. It lets async continuations notice that the voice has
    /// meanwhile been released and reused for another sound.</summary>
    public uint PlayId { get; private set; }

    /// <summary>The Time.time at which the voice started. Used by the steal policy.</summary>
    public float StartTime { get; private set; }

    /// <summary>The expected duration of the current clip at the current pitch, in seconds.</summary>
    public float ExpectedDuration { get; private set; }

    public int StealPriority => Sound != null ? Sound.StealPriority : 0;

    private void Awake() => EnsureSource();

    public void Configure(SoundEventSO sound, AudioClip clip, float volume, float pitch)
    {
        AudioSource source = EnsureSource();

        Sound = sound;
        IsLooping = sound.Loop;

        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = sound.Loop;
        source.outputAudioMixerGroup = sound.MixerGroup;
        source.spatialBlend = sound.Is3D ? 1f : 0f;
        source.rolloffMode = sound.RolloffMode;
        source.minDistance = sound.MinDistance;
        source.maxDistance = sound.MaxDistance;

        float safePitch = Mathf.Abs(pitch) > 0.01f ? Mathf.Abs(pitch) : 1f;
        ExpectedDuration = clip != null ? clip.length / safePitch : 0f;
    }

    public void Play()
    {
        StartTime = Time.time;
        PlayId++;
        EnsureSource().Play();
    }

    public override void OnReleased()
    {
        AudioSource source = EnsureSource();
        source.Stop();
        source.clip = null;

        Sound = null;
        FollowTarget = null;
        IsLooping = false;
        LoopHandle = AudioLoopHandle.None;
        ExpectedDuration = 0f;

        base.OnReleased();
    }

    private AudioSource EnsureSource()
    {
        if (_source == null)
        {
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();

            _source.playOnAwake = false;
            _source.dopplerLevel = 0f;
        }

        return _source;
    }
}
