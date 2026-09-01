using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Trigger PROVVISORIO per azzerare una delle due squadre e verificare il pannello di fine
/// combattimento. Vive nella scena di combattimento, non nella persistente, così i tasti non fanno
/// nulla mentre si è nel menu.
///
/// Esiste soprattutto per la SCONFITTA, che altrimenti è intestabile: non c'è modo di far morire
/// tutta la ciurma giocando.
///
/// Come <see cref="ReturnToMenuDebugTrigger"/> legge la tastiera direttamente invece di passare da
/// InputReader: è codice di servizio destinato a sparire, e aggiungere due azioni a GameInput per
/// qualcosa che verrà rimosso lascerebbe in giro action orfane.
/// </summary>
public class KillTeamDebugTrigger : MonoBehaviour
{
    [SerializeField] private TurnOrderDataSO _turnOrder;

    // Abbondantemente sopra qualunque pool di HP del gioco: serve solo a garantire la morte in un
    // colpo, non a essere un valore sensato.
    private const float LethalAmount = 9999f;

    // Riusato fra una pressione e l'altra: lo snapshot serve a ogni invocazione, non ha senso
    // riallocarlo.
    private readonly List<ITurnAgent> _snapshot = new();

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f9Key.wasPressedThisFrame) KillTeam(killEnemies: true);
        else if (keyboard.f10Key.wasPressedThisFrame) KillTeam(killEnemies: false);
    }

    /// <summary>
    /// Nemico = agente che è un <see cref="HostileCharacter"/>; qualunque altro ITurnAgent è per
    /// definizione un giocante. Stesso identico filtro di TurnOrderQueries, che è ciò che poi
    /// conterà i sopravvissuti.
    /// </summary>
    private void KillTeam(bool killEnemies)
    {
        if (_turnOrder == null) return;

        // Snapshot obbligatorio: TurnQueue avvolge la lista VIVA della coda, e la prima morte
        // arriva fino a TurnOrderDataSO.RemoveEntity in modo sincrono. Iterare direttamente la
        // coda mentre la si svuota lancerebbe InvalidOperationException al secondo agente.
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

            // Si passa dal danno vero e non da CombatOutcomeStateSO.Resolve: solo così scatta
            // OnDeath e con esso l'intera catena (OnCombatLeave, uscita dalla coda, animazione di
            // morte, valutazione dell'esito). Scorciatoie qui non verificherebbero nulla.
            health.TakeDamage(new DamagePayload(LethalAmount));
        }

        _snapshot.Clear();
    }
}
