using UnityEngine;

/// <summary>
/// Regista musicale di scena: livello sottile sopra i canali di loop esistenti.
/// Aggiunge un solo concetto rispetto a quanto gia' offre l'AudioDirector: esiste una sola
/// traccia musicale corrente, e chiederne un'altra sfuma la vecchia e accende la nuova.
///
/// Non tocca mai un AudioSource e non ha una pool propria: alza LoopSfxStartEventChannel /
/// LoopSfxStopEventChannel e lascia lavorare l'AudioDirector, che resta l'unico proprietario
/// delle voci audio.
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

        // I canali sono asset SO che sopravvivono alla scena: senza questo stop esplicito,
        // una traccia lasciata in loop continuerebbe a suonare oltre la vita di questo director
        // (stesso ragionamento del cleanup in AudioDirector.OnDisable).
        StopCurrent(0f);
    }

    private void HandleCue(MusicCue cue)
    {
        // Richiedere la traccia gia' in riproduzione non la fa ripartire da capo: e' anche
        // cio' che evita di scontrarsi con _maxConcurrentInstances = 1 dell'asset, che
        // rifiuterebbe silenziosamente il secondo Play mentre il primo e' ancora attivo.
        if (cue.Track == _currentTrack && _current.IsValid) return;

        StopCurrent(cue.FadeOutSeconds);

        if (cue.Track == null) return;

        if (!cue.Track.Loop)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[MusicDirector] '{cue.Track.name}' non e' marcato come Loop: " +
                "una traccia musicale deve avere _loop = true.", cue.Track);
#endif
            return;
        }

        if (_loopStartChannel == null) return;

        _current = AudioLoopHandle.New();
        _currentTrack = cue.Track;

        // Durante il crossfade convivono due voci sul gruppo Music: sono due SoundEventSO
        // diversi, quindi ciascuno resta entro il proprio _maxConcurrentInstances, e la pool
        // dell'AudioDirector (24 voci) ha ampio margine per ospitarle entrambe.
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
