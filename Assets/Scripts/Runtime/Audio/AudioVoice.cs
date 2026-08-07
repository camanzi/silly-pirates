using UnityEngine;

/// <summary>
/// Una voce audio prestabile dal pool. Wrappa un AudioSource e porta lo stato di dominio
/// (suono di origine, bersaglio da inseguire, loop) che al pool generico non interessa.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioVoice : PooledBehaviour
{
    private AudioSource _source;

    public AudioSource Source => EnsureSource();

    /// <summary>Il SoundEventSO che ha generato questa voce, o null se libera.</summary>
    public SoundEventSO Sound { get; private set; }

    /// <summary>Se valorizzato, l'AudioDirector copia la posizione del target ogni LateUpdate.</summary>
    public Transform FollowTarget { get; set; }

    public bool IsLooping { get; private set; }

    /// <summary>Handle del loop che sta suonando, o None. Serve a ripulire la mappa dei loop attivi
    /// anche quando la voce viene rilasciata da un percorso diverso dallo stop esplicito.</summary>
    public AudioLoopHandle LoopHandle { get; set; }

    /// <summary>Incrementato a ogni Play. Permette alle continuazioni asincrone di accorgersi
    /// che la voce e' stata nel frattempo rilasciata e riusata per un altro suono.</summary>
    public uint PlayId { get; private set; }

    /// <summary>Time.time in cui la voce e' partita. Usato dalla policy di steal.</summary>
    public float StartTime { get; private set; }

    /// <summary>Durata attesa della clip corrente al pitch corrente, in secondi.</summary>
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
