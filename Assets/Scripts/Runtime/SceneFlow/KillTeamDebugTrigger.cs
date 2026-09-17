using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A TEMPORARY trigger for wiping out one of the two teams and checking the end-of-combat panel. It
/// lives in the combat scene, not in the persistent one, so the keys do nothing while in the menu.
///
/// It exists mostly for DEFEAT, which is otherwise untestable: there is no way to get the whole crew
/// killed through normal play.
///
/// Like <see cref="ReturnToMenuDebugTrigger"/> it reads the keyboard directly instead of going through
/// InputReader: this is scaffolding destined to disappear, and adding two actions to GameInput for
/// something that will be removed would leave orphaned actions behind.
/// </summary>
public class KillTeamDebugTrigger : MonoBehaviour
{
    [SerializeField] private TurnOrderDataSO _turnOrder;

    // Comfortably above any HP pool in the game: it exists only to guarantee a one-shot kill, not to be
    // a sensible value.
    private const float LethalAmount = 9999f;

    // Reused between key presses: the snapshot is needed on every invocation, so there is no point
    // reallocating it.
    private readonly List<ITurnAgent> _snapshot = new();

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f9Key.wasPressedThisFrame) KillTeam(killEnemies: true);
        else if (keyboard.f10Key.wasPressedThisFrame) KillTeam(killEnemies: false);
    }

    /// <summary>
    /// An enemy is an agent that is a <see cref="HostileCharacter"/>; any other ITurnAgent is by
    /// definition a player character. Exactly the same filter as TurnOrderQueries, which is what will
    /// then count the survivors.
    /// </summary>
    private void KillTeam(bool killEnemies)
    {
        if (_turnOrder == null) return;

        // The snapshot is mandatory: TurnQueue wraps the queue's LIVE list, and the first death reaches
        // TurnOrderDataSO.RemoveEntity synchronously. Iterating the queue directly while emptying it
        // would throw an InvalidOperationException on the second agent.
        _snapshot.Clear();
        foreach (EntityTurnState state in _turnOrder.TurnQueue)
        {
            if (state.Agent == null) continue;
            bool isEnemy = state.Agent is HostileCharacter;
            if (isEnemy != killEnemies) continue;
            _snapshot.Add(state.Agent);
        }

        foreach (ITurnAgent agent in _snapshot)
        {
            HealthController health = agent.Health;
            if (health == null || !health.IsAlive) continue;

            // Real damage is used rather than CombatOutcomeStateSO.Resolve: only that way does OnDeath
            // fire, and with it the whole chain (OnCombatLeave, leaving the queue, the death animation,
            // the outcome evaluation). A shortcut here would verify nothing.
            health.TakeDamage(new DamagePayload(LethalAmount));
        }

        _snapshot.Clear();
    }
}
