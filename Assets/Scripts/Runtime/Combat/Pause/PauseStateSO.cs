using System;
using UnityEngine;

/// <summary>
/// The single source of truth for the in-combat pause. Whoever opens or closes the menu only writes
/// here; everyone who has to freeze (<see cref="PauseTimeController"/>, AudioDirector, FreeRoamTarget,
/// CombatStateManager) listens to <see cref="OnPauseChanged"/> or reads <see cref="IsPaused"/>.
///
/// Nobody owns Time.timeScale but PauseTimeController: keeping the state and its effect apart is what
/// lets a scene with no pause system at all behave exactly as before.
/// </summary>
[CreateAssetMenu(fileName = "PauseState", menuName = "Combat/Pause/Pause State")]
public class PauseStateSO : ScriptableObject, ICombatSessionResettable
{
    public bool IsPaused { get; private set; }

    public event Action<bool> OnPauseChanged;

    // A "pass-through" default: a scene with no pause menu must never start out frozen.
    private void OnEnable() => IsPaused = false;

    // Guards against the combat scene being unloaded while paused: the flag is not serialized, so
    // without this reset the next combat would begin with IsPaused still true — and, worse, with
    // PauseTimeController pulling a timeScale of 0 the moment it enables.
    public void ResetForNewCombat() => SetPaused(false);

    public void SetPaused(bool value)
    {
        if (IsPaused == value) return;

        IsPaused = value;
        OnPauseChanged?.Invoke(value);
    }
}
