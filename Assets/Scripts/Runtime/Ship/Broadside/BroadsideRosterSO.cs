using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One armed equipment waiting for the next Broadside, with the targets it picked when it awakened.
/// The targets are re-validated (and refilled) by <see cref="ShipBroadsideController"/> at firing time.
/// </summary>
public class BroadsideEntry
{
    public OffensiveEquipment Equipment { get; }
    public List<ITargettable> Targets { get; }

    public BroadsideEntry(OffensiveEquipment equipment, List<ITargettable> targets)
    {
        Equipment = equipment;
        Targets = targets ?? new List<ITargettable>();
    }
}

/// <summary>
/// The offensive equipment armed to fire during the ship's next turn (the "Broadside"), in awakening order.
///
/// Direct registration, in the same shape as <see cref="ShipOccluderRegistrySO"/>: an event channel would
/// still need a listener holding the list, and would lose the registrations made before it subscribed.
/// The equipment registers itself on awakening and unregisters when it leaves the active state (cooldown
/// after firing, an undone awakening, being disabled), so the roster never needs to be drained by hand.
///
/// Runtime state: it must be listed in CombatSession._resettables.
/// </summary>
[CreateAssetMenu(fileName = "BroadsideRoster", menuName = "Ship/Broadside/Broadside Roster")]
public class BroadsideRosterSO : ScriptableObject, ICombatSessionResettable
{
    [Header("Selection context")]
    [Tooltip("Read by the equipment to build the candidate list for its target selector")]
    [SerializeField] private TurnOrderDataSO _turnOrder;
    [Tooltip("Handed to the target selectors through BroadsideTargetingContext")]
    [SerializeField] private GridStateDataSO _gridState;

    private readonly List<BroadsideEntry> _entries = new();

    public TurnOrderDataSO TurnOrder => _turnOrder;
    public GridStateDataSO GridState => _gridState;
    public IReadOnlyList<BroadsideEntry> Entries => _entries;

    /// <summary>Raised on every change, e.g. for a future HUD preview of the broadside targets.</summary>
    public event Action OnRosterChanged;

    // A safety net for domain reloads, not the mechanism that holds scene transitions together.
    private void OnEnable() => _entries.Clear();

    public void ResetForNewCombat()
    {
        _entries.Clear();
        OnRosterChanged?.Invoke();
    }

    /// <summary>
    /// Adds the equipment, or replaces the targets of an equipment already armed (it keeps its place in the
    /// firing order).
    /// </summary>
    public void Register(OffensiveEquipment equipment, List<ITargettable> targets)
    {
        if (equipment == null) return;

        int index = IndexOf(equipment);
        var entry = new BroadsideEntry(equipment, targets);
        if (index >= 0) _entries[index] = entry;
        else _entries.Add(entry);

        OnRosterChanged?.Invoke();
    }

    public void Unregister(OffensiveEquipment equipment)
    {
        int index = IndexOf(equipment);
        if (index < 0) return;

        _entries.RemoveAt(index);
        OnRosterChanged?.Invoke();
    }

    public bool Contains(OffensiveEquipment equipment) => IndexOf(equipment) >= 0;

    /// <summary>
    /// Copies the entries into <paramref name="results"/>. Firing unregisters the equipment, so whoever iterates
    /// over the roster while shots are being executed must work on a snapshot.
    /// </summary>
    public void CopyEntries(List<BroadsideEntry> results)
    {
        results.Clear();
        results.AddRange(_entries);
    }

    private int IndexOf(OffensiveEquipment equipment)
    {
        if (equipment == null) return -1;
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Equipment == equipment) return i;
        return -1;
    }
}
