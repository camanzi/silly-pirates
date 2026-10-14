using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The awake equipment tied to the ship's turn (<see cref="IShipTurnParticipant"/>), in awakening order.
///
/// Same shape as <see cref="BroadsideRosterSO"/>: direct registration, done by <see cref="ShipEquipment"/>
/// itself when the equipment enters or leaves the active state. <see cref="ShipTurnAgent"/> reads its stat
/// modifiers from here and tells every participant when the ship has acted.
///
/// Runtime state: it must be listed in CombatSession._resettables.
/// </summary>
[CreateAssetMenu(fileName = "ShipTurnRoster", menuName = "Ship/Ship Turn Roster")]
public class ShipTurnRosterSO : ScriptableObject, ICombatSessionResettable
{
    private readonly List<IShipTurnParticipant> _participants = new();
    private readonly List<IShipTurnParticipant> _snapshot = new();

    public IReadOnlyList<IShipTurnParticipant> Participants => _participants;

    /// <summary>Raised on every change: the ship re-evaluates its stats on it.</summary>
    public event Action OnRosterChanged;

    // A safety net for domain reloads, not the mechanism that holds scene transitions together.
    private void OnEnable() => _participants.Clear();

    public void ResetForNewCombat()
    {
        _participants.Clear();
        OnRosterChanged?.Invoke();
    }

    public void Register(IShipTurnParticipant participant)
    {
        if (participant == null || _participants.Contains(participant)) return;
        _participants.Add(participant);
        OnRosterChanged?.Invoke();
    }

    public void Unregister(IShipTurnParticipant participant)
    {
        if (!_participants.Remove(participant)) return;
        OnRosterChanged?.Invoke();
    }

    public bool Contains(IShipTurnParticipant participant) => _participants.Contains(participant);

    /// <summary>
    /// Appends to <paramref name="results"/> every participant that is a <typeparamref name="T"/>. It does not
    /// clear the list, so the caller can merge these with modifiers from other sources.
    /// </summary>
    public void CollectModifiers<T>(List<T> results)
    {
        for (int i = 0; i < _participants.Count; i++)
            if (_participants[i] is T t) results.Add(t);
    }

    /// <summary>
    /// Tells every participant the ship has acted. Works on a snapshot: a participant going on cooldown
    /// unregisters itself during the call.
    /// </summary>
    public void NotifyShipTurnEnded()
    {
        _snapshot.Clear();
        _snapshot.AddRange(_participants);
        for (int i = 0; i < _snapshot.Count; i++)
        {
            // Called from inside the turn loop: one faulty participant must neither stop the others nor
            // bring the loop down.
            try { _snapshot[i].OnShipTurnEnded(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
        _snapshot.Clear();
    }
}
