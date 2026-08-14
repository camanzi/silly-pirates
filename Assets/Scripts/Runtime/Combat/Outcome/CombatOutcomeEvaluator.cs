using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Osserva la coda dei turni e decide quando il combattimento è vinto o perso, scrivendo l'esito su
/// <see cref="CombatOutcomeStateSO"/>. Vive nella scena di combattimento (non è un servizio persistente):
/// TurnOrderDataSO è già il registro dei vivi (HostileCharacter.OnCombatLeave rimuove l'agente dalla
/// coda come prima istruzione, prima di qualsiasi animazione), quindi non serve un secondo registry.
/// </summary>
public class CombatOutcomeEvaluator : MonoBehaviour
{
    [SerializeField] private TurnOrderDataSO _turnOrder;
    [SerializeField] private CombatIntroStateSO _introState;
    [SerializeField] private CombatOutcomeStateSO _outcomeState;
    [Tooltip("Attesa prima di risolvere l'esito, dopo aver rilevato che una delle due squadre è azzerata. " +
             "Serve perché la rimozione dalla coda è sincrona a T=0, ma l'ultimo nemico impiega ~0.8s ad " +
             "affondare (SinkUnderWaterAnimationSO): senza attesa il pannello di fine combattimento comparirebbe " +
             "sopra un nemico ancora visibile a schermo.")]
    [SerializeField] private float _resolveDelaySeconds = 1.2f;

    // Diventa true solo dopo aver osservato almeno una volta sia nemici che giocanti in coda.
    // Senza questa guardia, TurnController.Awake() chiama TurnOrderDataSO.Clear() che alza
    // OnQueueUpdated con la coda VUOTA: CountAliveEnemies() tornerebbe 0 e farebbe scattare una
    // "vittoria" fasulla all'avvio di ogni singolo combattimento, prima ancora che i nemici entrino
    // in coda.
    private bool _armed;

    // Protegge dal doppio avvio del ritardo: OnQueueUpdated è alzato più volte per turno
    // (StartActiveTurn, CompleteActiveTurn, AdjustAgentAV oltre ad Add/RemoveEntity), quindi senza
    // questa guardia partirebbero più Awaitable.WaitForSecondsAsync in parallelo per lo stesso esito.
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

    // Ci si iscrive a OnQueueUpdated e non al canale OnAgentLeave perché OnQueueUpdated è alzato da
    // AddEntity/RemoveEntity stessi: quando arriva, lo stato della coda è già aggiornato.
    // Iscrivendosi a OnAgentLeave si dipenderebbe invece dall'ordine dei listener rispetto a
    // TurnController.OnAgentLeave (che è quello che effettivamente chiama RemoveEntity).
    private void HandleQueueUpdated()
    {
        if (_turnOrder == null || _outcomeState == null) return;
        if (_outcomeState.IsCombatOver) return;

        // Durante l'intro la coda è ancora in fase di popolamento e il turn loop è fermo: valutare
        // qui darebbe letture parziali (es. solo la ciurma già spawnata, nessun nemico ancora).
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
            // Scena scaricata (o oggetto distrutto) prima che il ritardo scadesse: niente da
            // risolvere, la prossima sessione di combattimento riparte pulita via ResetForNewCombat.
        }
    }
}
