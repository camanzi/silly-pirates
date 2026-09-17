using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// The director of the combat intro sequence: enemies spawning one by one, the ship arriving from the
/// left, the turn loop being unblocked. Its script execution order is moved early (-1000) because
/// <see cref="Awake"/> has to raise <see cref="CombatIntroStateSO.IsIntroActive"/> BEFORE
/// <see cref="HostileCharacter.OnEnable"/> reads it.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class CombatIntroSequencer : MonoBehaviour
{
    [Header("State & Config")]
    [SerializeField] private CombatIntroStateSO _introState;
    [SerializeField] private CombatIntroSequenceSO _sequence;
    [SerializeField] private SpawnPointManagerSO _spawnPointManager;
    [SerializeField] private ShipControllerAnchorSO _shipAnchor;

    [Header("Camera")]
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;

    [Header("Events")]
    [SerializeField] private VoidEventChannel _onCombatStarted;
    [SerializeField] private StringEventChannel _flavorTextChannel;

    [Tooltip("Suppresses the world-space UI (the equipment radial menu) for the whole duration of the intro")]
    [SerializeField] private BoolEventChannel _showUIChannel;

    [Header("Audio")]
    [SerializeField] private MusicCueEventChannel _musicChannel;

    [Header("Debug")]
    [Tooltip("Skips the sequence: combat starts immediately, exactly as it does today")]
    [SerializeField] private bool _skipIntro;

    private ShipEntranceAnimator _shipEntrance;
    private bool _skipped;

    // True as soon as the combat music has been requested: from that moment on the sequencer no longer
    // owns it, and OnDestroy must not stop it.
    private bool _combatMusicStarted;

    private void Awake()
    {
        if (_skipIntro)
        {
            _skipped = true;
            return;
        }

        // Raised first, before any HostileCharacter OnEnable: it suppresses the automatic spawn.
        _introState?.BeginIntro();
    }

    private void OnEnable()
    {
        // The ship can NOT be resolved in Awake: this component's -1000 execution order makes it run
        // before any OnEnable, and therefore before ShipController registers itself.
        // Pull-then-subscribe covers both orderings; the first real use is in Start, which Unity
        // guarantees to run after every OnEnable.
        if (_shipAnchor == null) return;

        HandleShipChanged(_shipAnchor.Value);
        _shipAnchor.OnValueChanged += HandleShipChanged;
    }

    private void OnDisable()
    {
        if (_shipAnchor != null) _shipAnchor.OnValueChanged -= HandleShipChanged;
    }

    private void HandleShipChanged(ShipController ship)
        => _shipEntrance = ship != null ? ship.GetComponent<ShipEntranceAnimator>() : null;

    private void OnDestroy()
    {
        // A safety net: if the sequencer is destroyed mid-cinematic (a skipped beat, an exception, a
        // closed scene) the intro music would be left hanging in a loop, because nothing else stops it.
        // But if the fight has already begun, the current track is the combat one and no longer belongs
        // to this sequencer: it must be left alone.
        if (!_combatMusicStarted) _musicChannel?.RaiseEvent(MusicCue.Stop());
    }

    private async void Start()
    {
        if (_skipped)
        {
            // _skipIntro only switches off the cinematic, not the audio: skipping the sequence must not
            // leave the combat silent. MusicDirector.OnEnable runs before any Start() (this component's
            // -1000 execution order changes nothing for OnEnable), so the channel subscription is
            // already in place by this point.
            PlayCombatMusic();
            return;
        }

        // This belongs in Start and NOT in Awake: Unity runs every OnEnable of the objects present at
        // scene load before any Start, so by this point the GridElement children of the ship have already
        // resolved their tilemap with the ship at its docking position. Moving it earlier would make their
        // InitializePosition raycast miss the floor and leave them with no tilemap and no registered
        // occupancy.
        // Start still runs before the first render, so there is no frame showing the ship already docked.
        if (_shipEntrance == null)
            Debug.LogError(
                $"[{nameof(CombatIntroSequencer)}] No ship with a {nameof(ShipEntranceAnimator)} registered on " +
                $"{nameof(ShipControllerAnchorSO)}: the arrival beat will be skipped.", this);

        _shipEntrance?.SnapToEntry();

        try
        {
            await RunAsync(destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Destroyed during the sequence: nothing to play.
        }
        finally
        {
            // The intro must never be able to block the game or leave the UI suppressed, whatever fails
            // above.
            _showUIChannel?.RaiseEvent(true);

            _introState?.CompleteIntro();
        }
    }

    private async Awaitable RunAsync(CancellationToken token)
    {
        await Awaitable.NextFrameAsync(token); // lets the SpawnPoints' Start() run (claim via OverlapSphere)

        // This has to be raised AFTER the wait frame: CombatStateManager.Start() transitions to
        // IdleStateSO regardless of the intro and raises ShowUI(true), so doing it earlier would be
        // overwritten.
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

        // The combat music starts on the same beat as the "combat started" banner and the flavour text.
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
    /// A single cue does both things: it fades out the intro music still playing (if any) and brings up
    /// the combat soundtrack. From here on the track no longer belongs to this sequencer: the
    /// MusicDirector is the one owning its handle.
    /// </summary>
    private void PlayCombatMusic()
    {
        if (_sequence == null || _sequence.CombatMusic == null || _musicChannel == null) return;

        _musicChannel.RaiseEvent(
            MusicCue.Play(_sequence.CombatMusic, _sequence.CombatMusicFadeIn, _sequence.IntroMusicFadeOut));
        _combatMusicStarted = true;
    }

    /// <summary>
    /// Collects the enemies to show during the intro, ordered by increasing distance from the docking
    /// point. The single seam for a future dynamic spawner: replace this method to populate the sequence
    /// without touching the rest of the orchestration.
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
