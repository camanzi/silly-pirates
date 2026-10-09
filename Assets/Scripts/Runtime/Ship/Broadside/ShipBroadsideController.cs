using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Fires the Broadside: every offensive equipment armed in the <see cref="BroadsideRosterSO"/> shoots
/// during the ship's turn, quasi-simultaneously, in awakening order.
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

    [Header("Timing")]
    [Tooltip("Delay between the start of one shot and the next, in awakening order")]
    [SerializeField] [Min(0f)] private float _staggerSeconds = 0.25f;

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

    private readonly struct PlannedShot
    {
        public readonly OffensiveEquipment Equipment;
        public readonly AbilityBase Ability;
        public readonly List<ITargettable> Targets;

        public PlannedShot(OffensiveEquipment equipment, AbilityBase ability, List<ITargettable> targets)
        {
            Equipment = equipment;
            Ability = ability;
            Targets = targets;
        }
    }

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

            var shots = new List<BroadsideCommand.Shot>(planned.Count);
            for (int i = 0; i < planned.Count; i++)
            {
                var ability = (IBroadsideAbility)planned[i].Ability;
                ICommand command = ability.CreateBroadsideCommand(planned[i].Equipment, planned[i].Targets);
                shots.Add(new BroadsideCommand.Shot(command, i * _staggerSeconds));
            }

            _commandQueue.AddCommand(new BroadsideCommand(shots, token));
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

            List<ITargettable> targets = ResolveTargets(equipment, ability, entry.Targets);
            if (targets.Count == 0) continue;

            planned.Add(new PlannedShot(equipment, ability, targets));
        }

        return planned;
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
