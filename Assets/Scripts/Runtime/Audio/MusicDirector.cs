using UnityEngine;

/// <summary>
/// The scene's music director: a thin layer on top of the existing loop channels.
/// It adds exactly one concept to what the AudioDirector already offers: there is only ever one current
/// music track, and asking for another fades the old one out and brings the new one in.
///
/// It never touches an AudioSource and has no pool of its own: it raises LoopSfxStartEventChannel /
/// LoopSfxStopEventChannel and lets the AudioDirector do the work, which remains the sole owner of the
/// audio voices.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField] private MusicCueEventChannel _musicChannel;
    [SerializeField] private LoopSfxStartEventChannel _loopStartChannel;
    [SerializeField] private LoopSfxStopEventChannel _loopStopChannel;

    private AudioLoopHandle _current;
    private SoundEventSO _currentTrack;

    private void OnEnable()
    {
        if (_musicChannel != null) _musicChannel.OnEventRaised += HandleCue;
    }

    private void OnDisable()
    {
        if (_musicChannel != null) _musicChannel.OnEventRaised -= HandleCue;

        // The channels are SO assets that outlive the scene: without this explicit stop, a track left
        // looping would keep playing beyond this director's life (the same reasoning as the cleanup in
        // AudioDirector.OnDisable).
        StopCurrent(0f);
    }

    private void HandleCue(MusicCue cue)
    {
        // Asking for the track already playing does not restart it from the top: that is also what avoids
        // colliding with the asset's _maxConcurrentInstances = 1, which would silently refuse the second
        // Play while the first is still active.
        if (cue.Track == _currentTrack && _current.IsValid) return;

        StopCurrent(cue.FadeOutSeconds);

        if (cue.Track == null) return;

        if (!cue.Track.Loop)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[MusicDirector] '{cue.Track.name}' is not marked as Loop: " +
                "a music track has to have _loop = true.", cue.Track);
#endif
            return;
        }

        if (_loopStartChannel == null) return;

        _current = AudioLoopHandle.New();
        _currentTrack = cue.Track;

        // During the crossfade two voices coexist on the Music group: they are two different
        // SoundEventSO assets, so each stays within its own _maxConcurrentInstances, and the
        // AudioDirector's pool (24 voices) has ample room for both.
        _loopStartChannel.RaiseEvent(LoopSfxStartCue.TwoD(_current, cue.Track, fadeInSeconds: cue.FadeInSeconds));
    }

    private void StopCurrent(float fadeOutSeconds)
    {
        if (_current.IsValid && _loopStopChannel != null)
            _loopStopChannel.RaiseEvent(LoopSfxStopCue.Of(_current, fadeOutSeconds));

        _current = AudioLoopHandle.None;
        _currentTrack = null;
    }
}
