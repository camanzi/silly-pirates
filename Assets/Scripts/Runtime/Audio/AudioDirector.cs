using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// The scene's audio director: it listens to the cue channels and plays sounds using a pool of
/// AudioVoice.
///
/// Like CameraDirector, it is a scene MonoBehaviour that owns its objects directly (never inside a
/// ScriptableObject) and subscribes to the channels in OnEnable/OnDisable.
/// Unlike CameraDirector there is no handshake at all: audio never blocks the turn loop.
/// </summary>
public class AudioDirector : MonoBehaviour
{
    private const float ReleaseGraceSeconds = 0.05f;

    [Header("Channels")]
    [SerializeField] private SfxCueEventChannel _sfxChannel;
    [SerializeField] private LoopSfxStartEventChannel _loopStartChannel;
    [SerializeField] private LoopSfxStopEventChannel _loopStopChannel;

    [Header("Pool")]
    [Tooltip("Voices created at start-up")]
    [Min(0)] [SerializeField] private int _prewarmSize = 12;
    [Tooltip("Hard cap on voices. Past it the least important one-shot is stolen rather than growing the pool")]
    [Min(1)] [SerializeField] private int _maxPoolSize = 24;

    private ComponentPool<AudioVoice> _voices;
    private Transform _poolRoot;

    // Only the voices currently following a Transform: the others cost nothing per frame.
    private readonly List<AudioVoice> _followers = new();

    private readonly Dictionary<Guid, AudioVoice> _activeLoops = new();
    private readonly Dictionary<SoundEventSO, float> _lastPlayedAt = new();
    private readonly Dictionary<SoundEventSO, int> _liveCounts = new();
    private readonly Dictionary<SoundEventSO, AudioClip> _lastClip = new();

    private void Awake() => EnsureVoicePool();

    /// <summary>
    /// Lazy and idempotent, like <c>VfxDirector.EnsureRegistry</c>: a cue can arrive from the scene's
    /// initialization window (the OnEnable of a character entering combat), where the ordering between
    /// the Awake and OnEnable calls of different objects is not guaranteed.
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

        // The channels are SO assets that outlive the scene: the cleanup has to be complete.
        StopEverything();
    }

    private void OnDestroy() => _voices?.Clear();

    private void LateUpdate()
    {
        // LateUpdate and not Update: PrimeTween moves the visuals in Update, so the position has to be
        // copied afterwards.
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
                // The target was destroyed mid-playback: the voice goes back into the pool.
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
            Debug.LogWarning($"[AudioDirector] '{sound.name}' is marked as Loop: it has to be started from the loop channel, not as a one-shot.", sound);
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
            Debug.LogWarning("[AudioDirector] LoopSfxStartCue with no valid handle: use AudioLoopHandle.New().");
            return;
        }

        if (_activeLoops.ContainsKey(cue.Handle.Id)) return;
        if (!CanPlay(sound)) return;

        if (!sound.Loop)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioDirector] '{sound.name}' is not marked as Loop: it cannot be started from the loop channel.", sound);
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
        // A dictionary miss is a silent no-op: a double stop is a normal case.
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

    /// <summary>Takes a free voice, or applies the steal policy when the pool is exhausted.</summary>
    private AudioVoice AcquireVoice()
    {
        EnsureVoicePool();

        AudioVoice voice = _voices.Acquire();
        if (voice != null) return voice;

        AudioVoice victim = FindStealVictim();

        if (victim == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioDirector] Pool exhausted ({_voices.TotalCount} voices, all looping): cue dropped.", this);
#endif
            return null;
        }

        ReleaseVoice(victim);
        return _voices.Acquire();
    }

    /// <summary>
    /// The least important active one-shot: lowest priority first, and the oldest among ties.
    /// A loop is never stolen — whoever holds its handle must not have it invalidated under them.
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

        // A scale of 0 comes from payloads built by hand without the factory methods: it reads as "default".
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

    /// <summary>Centralized release: it updates the counters and removes the voice from every register.</summary>
    private void ReleaseVoice(AudioVoice voice)
    {
        if (voice == null || !voice.IsInUse) return;

        SoundEventSO sound = voice.Sound;
        if (sound != null && _liveCounts.TryGetValue(sound, out int live))
            _liveCounts[sound] = Mathf.Max(0, live - 1);

        if (voice.LoopHandle.IsValid) _activeLoops.Remove(voice.LoopHandle.Id);

        int index = _followers.IndexOf(voice);
        if (index >= 0) _followers.RemoveAt(index);

        // A fade still in flight would rewrite the volume of a voice that has already been reassigned.
        Tween.StopAll(voice.Source);

        _voices.Release(voice);
    }

    /// <summary>
    /// Returns a fixed-position one-shot to the pool once its clip is over.
    /// No per-frame cost: a single async continuation per voice.
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

        // The tween is deliberately not awaited: the same duration is waited out here with an Awaitable,
        // so the continuation stays cancellable through destroyCancellationToken.
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
