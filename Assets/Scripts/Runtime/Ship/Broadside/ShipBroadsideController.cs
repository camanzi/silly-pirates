using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Fires the Broadside: every offensive equipment armed in the <see cref="BroadsideRosterSO"/> shoots
/// during the ship's turn, in waves defined by the <see cref="BroadsideZoneTile"/> it stands on.
///
/// Firing order: all the equipment on priority-1 tiles fire together, and the next wave starts only once the
/// previous one has fully landed (one <see cref="BroadsideCommand"/> per wave, processed in sequence by the
/// command queue). Inside a wave the shots are simultaneous and keep the awakening order. An equipment that
/// does not stand on a zone tile is skipped with a warning and stays armed.
///
/// The targets were picked by each equipment when it awakened; here they are re-validated (dead or no
/// longer legal targets are dropped) and the missing slots refilled by asking that equipment's selector
/// again with the current candidates. An equipment left with nothing to shoot is skipped and stays armed.
///
/// Lives next to <see cref="ShipTurnAgent"/>, which drives it, the way EnemyTurnDriver sits next to
/// HostileCharacter. It follows the same turn shape: one camera cue, threat cues, the command queue, and
/// the closing of both in a finally.
/// </summary>
public class ShipBroadsideController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BroadsideRosterSO _roster;
    [SerializeField] private CommandQueueSO _commandQueue;

    [Header("Camera Direction")]
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [Tooltip("Profile of the single wide shot (ship + every living enemy) used for the whole broadside. " +
             "When empty the CameraDirector falls back to its default profile for FrameAll")]
    [SerializeField] private CameraCueProfileSO _broadsideCueProfile;
    [Tooltip("Empty child at the tip of the bow: frames the ship's front and gives the shot its heading")]
    [SerializeField] private Transform _bowAnchor;
    [Tooltip("Empty child at the tip of the stern: frames the ship's back")]
    [SerializeField] private Transform _sternAnchor;

    [Header("Threat")]
    [Tooltip("Announces every shot to its targets before the broadside starts, and closes them all at the end")]
    [SerializeField] private AbilityThreatEventChannel _threatChannel;

    private readonly List<BroadsideEntry> _snapshot = new();
    private readonly List<ITargettable> _refillBuffer = new();
    private readonly List<List<PlannedShot>> _waves = new();

    private readonly struct PlannedShot
    {
        public readonly OffensiveEquipment Equipment;
        public readonly AbilityBase Ability;
        public readonly List<ITargettable> Targets;
        public readonly int Priority;

        public PlannedShot(OffensiveEquipment equipment, AbilityBase ability, List<ITargettable> targets, int priority)
        {
            Equipment = equipment;
            Ability = ability;
            Targets = targets;
            Priority = priority;
        }
    }

    private static readonly System.Func<PlannedShot, int> PriorityOf = shot => shot.Priority;

    public bool HasArmedEquipment => _roster != null && _roster.Entries.Count > 0;

    /// <summary>
    /// Plans and fires the broadside, and returns once every shot has landed. Returns at once when nothing
    /// is armed or nothing can be shot. Never ends the turn itself: that stays with the caller.
    /// </summary>
    public async Awaitable ExecuteBroadsideAsync(CancellationToken token)
    {
        if (_roster == null || _commandQueue == null) return;

        // A snapshot: firing puts each equipment on cooldown, which unregisters it from the roster.
        _roster.CopyEntries(_snapshot);
        if (_snapshot.Count == 0) return;

        List<PlannedShot> planned = PlanShots();
        _snapshot.Clear();
        if (planned.Count == 0) return;

        bool focusBegun = false;
        try
        {
            // One Begin per shot, before anything moves: each reactor only answers the shot aimed at it.
            if (_threatChannel != null)
            {
                for (int i = 0; i < planned.Count; i++)
                    _threatChannel.RaiseEvent(AbilityThreatCue.Begin(planned[i].Ability, planned[i].Equipment, planned[i].Targets));
            }

            if (_cameraDirectorState != null && _cameraCueChannel != null)
            {
                focusBegun = true;
                await _cameraDirectorState.RaiseCueAndWaitAsync(_cameraCueChannel, BuildCameraCue(planned));
            }

            token.ThrowIfCancellationRequested();

            // One command per wave: the queue runs them in sequence, so a wave starts only once the previous
            // one has fully landed. Inside a wave every shot starts at once.
            BroadsideFiringOrder.BuildWaves(planned, PriorityOf, _waves);
            for (int w = 0; w < _waves.Count; w++)
            {
                List<PlannedShot> wave = _waves[w];
                var shots = new List<BroadsideCommand.Shot>(wave.Count);
                for (int i = 0; i < wave.Count; i++)
                {
                    var ability = (IBroadsideAbility)wave[i].Ability;
                    ICommand command = ability.CreateBroadsideCommand(wave[i].Equipment, wave[i].Targets);
                    shots.Add(new BroadsideCommand.Shot(command, 0f));
                }

                _commandQueue.AddCommand(new BroadsideCommand(shots, token));
            }
            _waves.Clear();

            await _commandQueue.ProcessQueueAsync();
        }
        finally
        {
            // Null-guarded: this runs in a finally and must not throw over whatever brought us here.
            _threatChannel?.RaiseEvent(AbilityThreatCue.End);
            if (focusBegun) _cameraDirectorState?.EndFocus();
        }
    }

    private List<PlannedShot> PlanShots()
    {
        var planned = new List<PlannedShot>(_snapshot.Count);

        for (int i = 0; i < _snapshot.Count; i++)
        {
            BroadsideEntry entry = _snapshot[i];
            OffensiveEquipment equipment = entry.Equipment;
            if (equipment == null || !equipment.isActiveAndEnabled || !equipment.IsAwake) continue;

            AbilityBase ability = equipment.BroadsideAbility;
            if (ability is not IBroadsideAbility)
            {
                Debug.LogWarning($"[{nameof(ShipBroadsideController)}] '{equipment.name}' is armed but its ability " +
                                 $"'{(ability != null ? ability.name : "none")}' does not implement {nameof(IBroadsideAbility)}: skipped.", equipment);
                continue;
            }

            if (!TryGetFiringPriority(equipment, out int priority)) continue;

            List<ITargettable> targets = ResolveTargets(equipment, ability, entry.Targets);
            if (targets.Count == 0) continue;

            planned.Add(new PlannedShot(equipment, ability, targets, priority));
        }

        return planned;
    }

    /// <summary>
    /// The firing priority of the <see cref="BroadsideZoneTile"/> under the equipment's anchor cell
    /// (<see cref="GridElement.gridPosition"/>): for a multi-cell footprint only the anchor's zone tile counts,
    /// the other cells are ignored. False (with a warning) when the equipment has no floor tilemap or its
    /// anchor does not stand on a zone tile: it is skipped and stays armed.
    /// </summary>
    private static bool TryGetFiringPriority(OffensiveEquipment equipment, out int priority)
    {
        priority = 0;
        var tilemap = equipment.activeTilemap;
        BroadsideZoneTile zone = tilemap != null ? tilemap.GetTile<BroadsideZoneTile>(equipment.gridPosition) : null;
        if (zone == null)
        {
            Debug.LogWarning($"[{nameof(ShipBroadsideController)}] '{equipment.name}' at cell {equipment.gridPosition} " +
                             $"does not stand on a {nameof(BroadsideZoneTile)}" +
                             (tilemap == null ? " (no floor tilemap)" : "") + ": skipped, it stays armed.", equipment);
            return false;
        }

        priority = zone.FiringPriority;
        return true;
    }

    /// <summary>
    /// The stored targets that are still legal, topped up to the ability's target count with fresh picks
    /// from the equipment's own selector. Always a new list: the command takes ownership of it.
    /// </summary>
    private List<ITargettable> ResolveTargets(OffensiveEquipment equipment, AbilityBase ability, List<ITargettable> stored)
    {
        int targetCount = BroadsideTargetingContext.ResolveTargetCount(ability);
        var targets = new List<ITargettable>(targetCount);

        for (int i = 0; i < stored.Count && targets.Count < targetCount; i++)
        {
            if (BroadsideCandidateQuery.IsLegalTarget(equipment, ability, stored[i]))
                targets.Add(stored[i]);
        }

        int missing = targetCount - targets.Count;
        if (missing <= 0) return targets;

        equipment.SelectTargets(_refillBuffer);
        for (int i = 0; i < _refillBuffer.Count && missing > 0; i++, missing--)
            targets.Add(_refillBuffer[i]);
        _refillBuffer.Clear();

        return targets;
    }

    /// <summary>
    /// One wide shot for the whole broadside: the whole ship (bow and stern as fixed points) and every
    /// living enemy, not only the ones being shot, seen from the bow side. Caster-less: the ship is not an
    /// interactable, and the bow/stern points already put it in the frame. The heading (stern to bow) lets
    /// the profile aim the shot relative to the ship rather than to the world.
    /// When an anchor is missing the shot degrades to the ship's position and the authored angle.
    /// </summary>
    private AbilityExecutionCue BuildCameraCue(List<PlannedShot> planned)
    {
        var enemies = new List<ITargettable>();
        CollectLivingEnemies(enemies);

        // Never an empty frame: should the turn queue yield nothing, the shot targets fall back on the
        // union of the planned targets.
        if (enemies.Count == 0)
        {
            for (int i = 0; i < planned.Count; i++)
            {
                List<ITargettable> targets = planned[i].Targets;
                for (int t = 0; t < targets.Count; t++)
                    if (!enemies.Contains(targets[t])) enemies.Add(targets[t]);
            }
        }

        List<Vector3> shipPoints;
        Vector3? heading = null;
        if (_bowAnchor != null && _sternAnchor != null)
        {
            shipPoints = new List<Vector3>(2) { _bowAnchor.position, _sternAnchor.position };
            heading = _bowAnchor.position - _sternAnchor.position;
        }
        else
        {
            Debug.LogWarning($"[{nameof(ShipBroadsideController)}] Bow/stern anchors not assigned: the broadside " +
                             "shot frames the ship's pivot from the authored camera angle.", this);
            shipPoints = new List<Vector3>(1) { transform.position };
        }

        return new AbilityExecutionCue(null, null, enemies, shipPoints, null)
        {
            CueTypeOverride = CameraCueType.FrameAll,
            ProfileOverride = _broadsideCueProfile,
            ShotHeading = heading
        };
    }

    private void CollectLivingEnemies(List<ITargettable> results)
    {
        TurnOrderDataSO turnOrder = _roster != null ? _roster.TurnOrder : null;
        if (turnOrder == null) return;

        foreach (EntityTurnState state in turnOrder.TurnQueue)
        {
            if (state.Agent is not HostileCharacter) continue;
            if (state.Agent is not ITargettable target) continue;
            if (!BroadsideCandidateQuery.IsAlive(target)) continue;
            if (!results.Contains(target)) results.Add(target);
        }
    }
}
