using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Regista della sequenza di intro al combattimento: spawn dei nemici uno per uno, arrivo della
/// nave da sinistra, sblocco del turn loop. Ordine di esecuzione anticipato (-1000) perché
/// <see cref="Awake"/> deve alzare <see cref="CombatIntroStateSO.IsIntroActive"/> PRIMA che
/// <see cref="HostileCharacter.OnEnable"/> lo consulti.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class CombatIntroSequencer : MonoBehaviour
{
    [Header("Stato & Config")]
    [SerializeField] private CombatIntroStateSO _introState;
    [SerializeField] private CombatIntroSequenceSO _sequence;
    [SerializeField] private SpawnPointManagerSO _spawnPointManager;

    [Header("Camera")]
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;

    [Header("Eventi")]
    [SerializeField] private VoidEventChannel _onCombatStarted;
    [SerializeField] private StringEventChannel _flavorTextChannel;

    [Tooltip("Sopprime la UI world-space (menu radiale degli equipaggiamenti) per tutta la durata dell'intro")]
    [SerializeField] private BoolEventChannel _showUIChannel;

    [Header("Audio")]
    [SerializeField] private MusicCueEventChannel _musicChannel;

    [Header("Debug")]
    [Tooltip("Salta la sequenza: il combattimento parte subito, come oggi")]
    [SerializeField] private bool _skipIntro;

    private ShipEntranceAnimator _shipEntrance;
    private bool _skipped;

    // True non appena la musica di combattimento e' stata richiesta: da quel momento il
    // sequencer non ne e' piu' proprietario, e OnDestroy non deve fermarla.
    private bool _combatMusicStarted;

    private void Awake()
    {
        if (_skipIntro)
        {
            _skipped = true;
            return;
        }

        // Alzato per primo, prima di qualunque OnEnable di HostileCharacter: sopprime lo spawn automatico.
        _introState?.BeginIntro();

        var shipController = FindFirstObjectByType<ShipController>();
        _shipEntrance = shipController != null ? shipController.GetComponent<ShipEntranceAnimator>() : null;

        if (_shipEntrance == null)
            Debug.LogError(
                $"[{nameof(CombatIntroSequencer)}] Nessuna nave con {nameof(ShipEntranceAnimator)} trovata in scena: " +
                "il beat di arrivo verrà saltato.", this);
    }

    private void OnDestroy()
    {
        // Rete di sicurezza: se il sequencer viene distrutto a meta' cinematica (beat saltato,
        // eccezione, scena chiusa) la musica d'intro resterebbe appesa in loop perche' nessun
        // altro la ferma. Ma se il fight e' gia' partito, la traccia corrente e' quella di
        // combattimento e non appartiene piu' a questo sequencer: non va toccata.
        if (!_combatMusicStarted) _musicChannel?.RaiseEvent(MusicCue.Stop());
    }

    private async void Start()
    {
        if (_skipped)
        {
            // _skipIntro spegne solo la cinematica, non l'audio: saltare la sequenza non deve
            // lasciare il combattimento muto. MusicDirector.OnEnable gira prima di qualunque
            // Start() (l'ordine di esecuzione -1000 di questo componente non cambia le cose per
            // OnEnable), quindi la sottoscrizione al canale c'e' gia' a questo punto.
            PlayCombatMusic();
            return;
        }

        // Deve stare in Start e NON in Awake: Unity esegue tutti gli OnEnable degli oggetti presenti
        // al caricamento scena prima di qualunque Start, quindi qui i GridElement figli della nave
        // hanno già risolto la loro tilemap con la nave alla posizione di attracco. Spostandola
        // prima, il loro raycast di InitializePosition mancherebbe il pavimento e li lascerebbe
        // senza tilemap e senza occupancy registrata.
        // Start gira comunque prima del primo render, quindi non esiste un frame con la nave attraccata.
        _shipEntrance?.SnapToEntry();

        try
        {
            await RunAsync(destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Distrutto durante la sequenza: nulla da riprodurre.
        }
        finally
        {
            // L'intro non deve mai poter bloccare il gioco né lasciare la UI soppressa,
            // qualunque cosa fallisca sopra.
            _showUIChannel?.RaiseEvent(true);

            _introState?.CompleteIntro();
        }
    }

    private async Awaitable RunAsync(CancellationToken token)
    {
        await Awaitable.NextFrameAsync(token); // lascia girare gli Start() degli SpawnPoint (claim via OverlapSphere)

        // Va alzato DOPO il frame di attesa: CombatStateManager.Start() transisce all'IdleStateSO
        // indipendentemente dall'intro e alza ShowUI(true), quindi farlo prima verrebbe sovrascritto.
        _showUIChannel?.RaiseEvent(false);

        StartIntroMusic();

        await Hold(_sequence != null ? _sequence.PreSequenceDelay : 0f, token);

        List<IntroSpawnEntry> entries = CollectIntroEnemies();
        for (int i = 0; i < entries.Count; i++)
        {
            IntroSpawnEntry entry = entries[i];
            if (entry.Enemy == null || entry.Enemy.LifecycleAnimator == null) continue;

            await RunEnemyBeatAsync(entry, token);
        }

        if (_shipEntrance != null)
            await RunShipBeatAsync(token);

        await Hold(_sequence != null ? _sequence.PostDockDelay : 0f, token);

        _directorState?.EndFocus();
        _onCombatStarted?.RaiseEvent();
        if (_sequence != null) _flavorTextChannel?.RaiseEvent(_sequence.OpeningLine);

        // La musica di combattimento parte sullo stesso beat del banner "combattimento iniziato"
        // e della flavor text.
        PlayCombatMusic();
    }

    private async Awaitable RunEnemyBeatAsync(IntroSpawnEntry entry, CancellationToken token)
    {
        if (_directorState != null && _cueChannel != null)
        {
            var cue = new AbilityExecutionCue(null, null, null, null, entry.Point.Position)
            {
                CueTypeOverride = CameraCueType.FocusArea,
                ProfileOverride = _sequence != null ? _sequence.EnemyFocusProfile : null
            };
            await _directorState.RaiseCueAndWaitAsync(_cueChannel, cue);
        }

        await Hold(_sequence != null ? _sequence.PerEnemyPreHold : 0f, token);
        await entry.Enemy.LifecycleAnimator.PlayAsync(LifecyclePhase.Spawn, token);
        await Hold(_sequence != null ? _sequence.PerEnemyPostHold : 0f, token);
    }

    private async Awaitable RunShipBeatAsync(CancellationToken token)
    {
        if (_directorState != null && _cueChannel != null)
        {
            var cue = new AbilityExecutionCue(null, null, null, null, _shipEntrance.DockPosition)
            {
                CueTypeOverride = CameraCueType.FocusArea,
                ProfileOverride = _sequence != null ? _sequence.ShipFocusProfile : null
            };
            await _directorState.RaiseCueAndWaitAsync(_cueChannel, cue);
        }

        await _shipEntrance.ArriveAsync(token);
        await Hold(_sequence != null ? _sequence.ShipArrivalHold : 0f, token);
    }

    private static async Awaitable Hold(float seconds, CancellationToken token)
    {
        if (seconds > 0f) await Awaitable.WaitForSecondsAsync(seconds, token);
    }

    private void StartIntroMusic()
    {
        if (_sequence == null || _sequence.IntroMusic == null || _musicChannel == null) return;

        _musicChannel.RaiseEvent(MusicCue.Play(_sequence.IntroMusic, _sequence.IntroMusicFadeIn));
    }

    /// <summary>
    /// Una sola cue fa entrambe le cose: sfuma la musica d'intro ancora in riproduzione (se
    /// presente) e accende la soundtrack di combattimento. Da qui in poi la traccia non
    /// appartiene piu' a questo sequencer: e' il MusicDirector a possederne l'handle.
    /// </summary>
    private void PlayCombatMusic()
    {
        if (_sequence == null || _sequence.CombatMusic == null || _musicChannel == null) return;

        _musicChannel.RaiseEvent(
            MusicCue.Play(_sequence.CombatMusic, _sequence.CombatMusicFadeIn, _sequence.IntroMusicFadeOut));
        _combatMusicStarted = true;
    }

    /// <summary>
    /// Raccoglie i nemici da mostrare in intro, ordinati per distanza crescente dal punto di
    /// attracco. Unico seam per un futuro spawner dinamico: sostituire questo metodo per popolare
    /// la sequenza senza toccare il resto dell'orchestrazione.
    /// </summary>
    protected virtual List<IntroSpawnEntry> CollectIntroEnemies()
    {
        var entries = new List<IntroSpawnEntry>();
        if (_spawnPointManager == null) return entries;

        foreach (SpawnPoint point in _spawnPointManager.RegisteredPoints)
        {
            if (point == null || !point.IsOccupied) continue;
            entries.Add(new IntroSpawnEntry(point, point.Occupant));
        }

        Vector3 origin = _shipEntrance != null ? _shipEntrance.DockPosition : transform.position;
        entries.Sort((a, b) =>
            Vector3.SqrMagnitude(a.Point.Position - origin)
                .CompareTo(Vector3.SqrMagnitude(b.Point.Position - origin)));

        return entries;
    }

    protected readonly struct IntroSpawnEntry
    {
        public readonly SpawnPoint Point;
        public readonly HostileCharacter Enemy;

        public IntroSpawnEntry(SpawnPoint point, HostileCharacter enemy)
        {
            Point = point;
            Enemy = enemy;
        }
    }
}
