using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Regista audio di scena: ascolta i canali di cue e riproduce i suoni usando un pool di AudioVoice.
///
/// Come CameraDirector, e' un MonoBehaviour di scena che possiede direttamente i propri oggetti
/// (mai dentro un ScriptableObject) e si iscrive ai canali in OnEnable/OnDisable.
/// A differenza di CameraDirector non esiste nessun handshake: l'audio non blocca mai il turn loop.
/// </summary>
public class AudioDirector : MonoBehaviour
{
    private const float ReleaseGraceSeconds = 0.05f;

    [Header("Channels")]
    [SerializeField] private SfxCueEventChannel _sfxChannel;
    [SerializeField] private LoopSfxStartEventChannel _loopStartChannel;
    [SerializeField] private LoopSfxStopEventChannel _loopStopChannel;

    [Header("Pool")]
    [Tooltip("Voci create all'avvio")]
    [Min(0)] [SerializeField] private int _prewarmSize = 12;
    [Tooltip("Tetto massimo di voci. Oltre questo si ruba il one-shot meno importante, non si cresce")]
    [Min(1)] [SerializeField] private int _maxPoolSize = 24;

    private ComponentPool<AudioVoice> _voices;
    private Transform _poolRoot;

    // Solo le voci che stanno inseguendo un Transform: le altre non costano nulla per frame.
    private readonly List<AudioVoice> _followers = new();

    private readonly Dictionary<Guid, AudioVoice> _activeLoops = new();
    private readonly Dictionary<SoundEventSO, float> _lastPlayedAt = new();
    private readonly Dictionary<SoundEventSO, int> _liveCounts = new();
    private readonly Dictionary<SoundEventSO, AudioClip> _lastClip = new();

    private void Awake() => EnsureVoicePool();

    /// <summary>
    /// Lazy e idempotente, come <c>VfxDirector.EnsureRegistry</c>: un cue puo' arrivare dalla finestra
    /// di inizializzazione della scena (l'OnEnable di un personaggio che entra in combattimento), dove
    /// l'ordine fra gli Awake e gli OnEnable di oggetti diversi non e' garantito.
    /// </summary>
    private void EnsureVoicePool()
    {
        if (_voices != null) return;

        _poolRoot = new GameObject("AudioVoicePool").transform;
        _poolRoot.SetParent(transform, false);

        _voices = new ComponentPool<AudioVoice>(CreateVoice, _poolRoot, _prewarmSize, _maxPoolSize);
    }

    private void OnEnable()
    {
        if (_sfxChannel != null) _sfxChannel.OnEventRaised += HandleSfxCue;
        if (_loopStartChannel != null) _loopStartChannel.OnEventRaised += HandleLoopStart;
        if (_loopStopChannel != null) _loopStopChannel.OnEventRaised += HandleLoopStop;
    }

    private void OnDisable()
    {
        if (_sfxChannel != null) _sfxChannel.OnEventRaised -= HandleSfxCue;
        if (_loopStartChannel != null) _loopStartChannel.OnEventRaised -= HandleLoopStart;
        if (_loopStopChannel != null) _loopStopChannel.OnEventRaised -= HandleLoopStop;

        // I canali sono asset SO che sopravvivono alla scena: il cleanup deve essere completo.
        StopEverything();
    }

    private void OnDestroy() => _voices?.Clear();

    private void LateUpdate()
    {
        // LateUpdate e non Update: PrimeTween muove i visual in Update, la posizione va copiata dopo.
        if (_followers.Count == 0) return;

        for (int i = _followers.Count - 1; i >= 0; i--)
        {
            AudioVoice voice = _followers[i];

            if (voice == null || !voice.IsInUse)
            {
                _followers.RemoveAt(i);
                continue;
            }

            if (voice.FollowTarget == null)
            {
                // Il bersaglio e' stato distrutto a meta' riproduzione: la voce torna nel pool.
                _followers.RemoveAt(i);
                ReleaseVoice(voice);
                continue;
            }

            voice.transform.position = voice.FollowTarget.position;

            if (!voice.IsLooping && !voice.Source.isPlaying)
            {
                _followers.RemoveAt(i);
                ReleaseVoice(voice);
            }
        }
    }

    private void HandleSfxCue(SfxCue cue)
    {
        SoundEventSO sound = cue.Sound;
        if (!CanPlay(sound)) return;

        if (sound.Loop)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioDirector] '{sound.name}' e' marcato come Loop: va avviato dal canale di loop, non come one-shot.", sound);
#endif
            return;
        }

        AudioVoice voice = AcquireVoice();
        if (voice == null) return;

        if (!TryPrepareVoice(voice, sound, cue.VolumeScale, cue.PitchScale, out _))
        {
            _voices.Release(voice);
            return;
        }

        if (cue.FollowTarget != null)
        {
            voice.FollowTarget = cue.FollowTarget;
            voice.transform.position = cue.FollowTarget.position;
            _followers.Add(voice);
            voice.Play();
            return;
        }

        voice.transform.position = cue.WorldPosition ?? Vector3.zero;
        voice.Play();
        ScheduleRelease(voice);
    }

    private void HandleLoopStart(LoopSfxStartCue cue)
    {
        SoundEventSO sound = cue.Sound;

        if (!cue.Handle.IsValid)
        {
            Debug.LogWarning("[AudioDirector] LoopSfxStartCue senza handle valido: usa AudioLoopHandle.New().");
            return;
        }

        if (_activeLoops.ContainsKey(cue.Handle.Id)) return;
        if (!CanPlay(sound)) return;

        if (!sound.Loop)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioDirector] '{sound.name}' non e' marcato come Loop: non puo' essere avviato dal canale di loop.", sound);
#endif
            return;
        }

        AudioVoice voice = AcquireVoice();
        if (voice == null) return;

        if (!TryPrepareVoice(voice, sound, cue.VolumeScale, 1f, out float volume))
        {
            _voices.Release(voice);
            return;
        }

        voice.LoopHandle = cue.Handle;
        _activeLoops[cue.Handle.Id] = voice;

        if (cue.FollowTarget != null)
        {
            voice.FollowTarget = cue.FollowTarget;
            voice.transform.position = cue.FollowTarget.position;
            _followers.Add(voice);
        }
        else
        {
            voice.transform.position = cue.WorldPosition ?? Vector3.zero;
        }

        if (cue.FadeInSeconds > 0f)
        {
            voice.Source.volume = 0f;
            voice.Play();
            Tween.AudioVolume(voice.Source, volume, cue.FadeInSeconds);
            return;
        }

        voice.Play();
    }

    private void HandleLoopStop(LoopSfxStopCue cue)
    {
        // Miss del dizionario = no-op silenzioso: il doppio stop e' un caso normale.
        if (!_activeLoops.TryGetValue(cue.Handle.Id, out AudioVoice voice)) return;

        _activeLoops.Remove(cue.Handle.Id);

        if (voice == null || !voice.IsInUse) return;

        voice.LoopHandle = AudioLoopHandle.None;

        if (cue.FadeOutSeconds > 0f)
        {
            FadeOutAndRelease(voice, cue.FadeOutSeconds);
            return;
        }

        ReleaseVoice(voice);
    }

    /// <summary>Preleva una voce libera, o applica la policy di steal se il pool e' esaurito.</summary>
    private AudioVoice AcquireVoice()
    {
        EnsureVoicePool();

        AudioVoice voice = _voices.Acquire();
        if (voice != null) return voice;

        AudioVoice victim = FindStealVictim();

        if (victim == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioDirector] Pool esaurito ({_voices.TotalCount} voci, tutte in loop): cue scartato.", this);
#endif
            return null;
        }

        ReleaseVoice(victim);
        return _voices.Acquire();
    }

    /// <summary>
    /// Il one-shot attivo meno importante: prima la priorita' piu' bassa, a parita' il piu' vecchio.
    /// Un loop non viene mai rubato — chi ne possiede l'handle non deve vederselo invalidare.
    /// </summary>
    private AudioVoice FindStealVictim()
    {
        IReadOnlyList<AudioVoice> active = _voices.Active;
        AudioVoice victim = null;

        for (int i = 0; i < active.Count; i++)
        {
            AudioVoice candidate = active[i];
            if (candidate == null || candidate.IsLooping) continue;

            if (victim == null)
            {
                victim = candidate;
                continue;
            }

            if (candidate.StealPriority < victim.StealPriority) victim = candidate;
            else if (candidate.StealPriority == victim.StealPriority && candidate.StartTime < victim.StartTime) victim = candidate;
        }

        return victim;
    }

    private bool CanPlay(SoundEventSO sound)
    {
        if (sound == null || !sound.HasClips) return false;

        if (sound.CooldownSeconds > 0f
            && _lastPlayedAt.TryGetValue(sound, out float last)
            && Time.time - last < sound.CooldownSeconds)
            return false;

        if (_liveCounts.TryGetValue(sound, out int live) && live >= sound.MaxConcurrentInstances)
            return false;

        return true;
    }

    private bool TryPrepareVoice(AudioVoice voice, SoundEventSO sound, float volumeScale, float pitchScale, out float volume)
    {
        volume = 0f;

        _lastClip.TryGetValue(sound, out AudioClip previous);
        AudioClip clip = sound.PickClip(previous);
        if (clip == null) return false;

        // Uno scale a 0 viene dai payload costruiti a mano senza i factory method: si legge come "default".
        float safeVolumeScale = volumeScale > 0f ? volumeScale : 1f;
        float safePitchScale = pitchScale > 0f ? pitchScale : 1f;

        volume = Mathf.Clamp01(sound.PickVolume() * safeVolumeScale);
        float pitch = sound.PickPitch() * safePitchScale;

        voice.Configure(sound, clip, volume, pitch);

        _lastClip[sound] = clip;
        _lastPlayedAt[sound] = Time.time;
        _liveCounts.TryGetValue(sound, out int live);
        _liveCounts[sound] = live + 1;

        return true;
    }

    /// <summary>Rilascio centralizzato: aggiorna i conteggi e sfila la voce da ogni registro.</summary>
    private void ReleaseVoice(AudioVoice voice)
    {
        if (voice == null || !voice.IsInUse) return;

        SoundEventSO sound = voice.Sound;
        if (sound != null && _liveCounts.TryGetValue(sound, out int live))
            _liveCounts[sound] = Mathf.Max(0, live - 1);

        if (voice.LoopHandle.IsValid) _activeLoops.Remove(voice.LoopHandle.Id);

        int index = _followers.IndexOf(voice);
        if (index >= 0) _followers.RemoveAt(index);

        // Un fade ancora in volo riscriverebbe il volume di una voce gia' riassegnata.
        Tween.StopAll(voice.Source);

        _voices.Release(voice);
    }

    /// <summary>
    /// Riporta nel pool un one-shot posizionale fisso quando la clip e' finita.
    /// Nessun costo per frame: una sola continuazione asincrona per voce.
    /// </summary>
    private async void ScheduleRelease(AudioVoice voice)
    {
        uint playId = voice.PlayId;
        float delay = voice.ExpectedDuration + ReleaseGraceSeconds;

        try
        {
            await Awaitable.WaitForSecondsAsync(delay, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (voice == null || !voice.IsInUse || voice.PlayId != playId) return;
        ReleaseVoice(voice);
    }

    private async void FadeOutAndRelease(AudioVoice voice, float seconds)
    {
        uint playId = voice.PlayId;

        // Il tween e' volutamente non atteso: qui si aspetta la stessa durata con Awaitable,
        // cosi' la continuazione resta cancellabile con destroyCancellationToken.
        _ = Tween.AudioVolume(voice.Source, 0f, seconds);

        try
        {
            await Awaitable.WaitForSecondsAsync(seconds, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (voice == null || !voice.IsInUse || voice.PlayId != playId) return;
        ReleaseVoice(voice);
    }

    private void StopEverything()
    {
        _followers.Clear();
        _activeLoops.Clear();
        _liveCounts.Clear();
        _voices?.ReleaseAll();
    }

    private AudioVoice CreateVoice() => new GameObject("AudioVoice").AddComponent<AudioVoice>();
}
