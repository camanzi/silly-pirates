using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Watches the turn queue and decides when the combat is won or lost, writing the outcome to
/// <see cref="CombatOutcomeStateSO"/>. It lives in the combat scene (it is not a persistent service):
/// TurnOrderDataSO is already the register of the living (HostileCharacter.OnCombatLeave removes the
/// agent from the queue as its very first statement, before any animation), so no second registry is
/// needed.
/// </summary>
public class CombatOutcomeEvaluator : MonoBehaviour
{
    [SerializeField] private TurnOrderDataSO _turnOrder;
    [SerializeField] private CombatIntroStateSO _introState;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;
    [Tooltip("Wait before resolving the outcome, once one of the two teams has been detected as wiped. " +
             "It is needed because removal from the queue is synchronous at T=0, while the last enemy " +
             "takes ~0.8s to sink (SinkUnderWaterAnimationSO): without the wait, the end-of-combat panel " +
             "would appear over an enemy still visible on screen.")]
    [SerializeField] private float _resolveDelaySeconds = 1.2f;

    // Becomes true only after both enemies and player characters have been observed in the queue at
    // least once. Without this guard, TurnController.Awake() calls TurnOrderDataSO.Clear(), which raises
    // OnQueueUpdated with an EMPTY queue: CountAliveEnemies() would return 0 and trigger a bogus
    // "victory" at the start of every single combat, before the enemies have even entered the queue.
    private bool _armed;

    // Guards against starting the delay twice: OnQueueUpdated is raised several times per turn
    // (StartActiveTurn, CompleteActiveTurn and AdjustAgentAV on top of Add/RemoveEntity), so without this
    // guard several Awaitable.WaitForSecondsAsync would run in parallel for the same outcome.
    private bool _resolvePending;

    private void OnEnable()
    {
        if (_turnOrder != null && _turnOrder.OnQueueUpdated != null)
            _turnOrder.OnQueueUpdated.OnEventRaised += HandleQueueUpdated;
    }

    private void OnDisable()
    {
        if (_turnOrder != null && _turnOrder.OnQueueUpdated != null)
            _turnOrder.OnQueueUpdated.OnEventRaised -= HandleQueueUpdated;
    }

    // OnQueueUpdated is subscribed to rather than the OnAgentLeave channel because OnQueueUpdated is
    // raised by AddEntity/RemoveEntity themselves: by the time it arrives, the queue's state is already
    // up to date. Subscribing to OnAgentLeave would instead depend on the ordering of listeners relative
    // to TurnController.OnAgentLeave (which is what actually calls RemoveEntity).
    private void HandleQueueUpdated()
    {
        if (_turnOrder == null || _outcomeState == null) return;
        if (_outcomeState.IsCombatOver) return;

        // During the intro the queue is still being populated and the turn loop is stopped: evaluating
        // here would give partial readings (e.g. only the crew spawned so far, no enemies yet).
        if (_introState != null && !_introState.IsCombatReady) return;

        int aliveEnemies = _turnOrder.CountAliveEnemies();
        int alivePlayers = _turnOrder.CountAlivePlayers();

        if (!_armed)
        {
            if (aliveEnemies > 0 && alivePlayers > 0) _armed = true;
            else return;
        }

        if (_resolvePending) return;

        if (aliveEnemies == 0)
        {
            _resolvePending = true;
            _ = ResolveAfterDelayAsync(CombatOutcome.Victory, destroyCancellationToken);
        }
        else if (alivePlayers == 0)
        {
            _resolvePending = true;
            _ = ResolveAfterDelayAsync(CombatOutcome.Defeat, destroyCancellationToken);
        }
    }

    private async Awaitable ResolveAfterDelayAsync(CombatOutcome outcome, CancellationToken token)
    {
        try
        {
            await Awaitable.WaitForSecondsAsync(_resolveDelaySeconds, token);
            _outcomeState.Resolve(outcome);
        }
        catch (OperationCanceledException)
        {
            // The scene was unloaded (or the object destroyed) before the delay elapsed: nothing to
            // resolve, the next combat session starts clean via ResetForNewCombat.
        }
    }
}
