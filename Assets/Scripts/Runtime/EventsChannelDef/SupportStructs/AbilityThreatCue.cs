using System.Collections.Generic;

/// <summary>
/// Annuncia che un'abilita' sta per essere eseguita contro dei bersagli, e che l'esecuzione e' finita.
/// Alzata nei due (soli) punti dove un comando viene messo in coda ed eseguito: ExecutionStateSO per il
/// giocatore, EnemyTurnDriver per i nemici.
///
/// Serve ai bersagli per reagire PRIMA dell'impatto — il danno arriva tardi dentro il comando, dopo il volo
/// del proiettile — cosa che un HealthBehaviorSO, che vede il colpo solo quando e' gia' arrivato, non puo'
/// fare. Gemella di AbilityExecutionCue, che porta gli stessi dati alla regia di camera: separata perche'
/// quella non ha un evento di chiusura e legarsi al director della camera sarebbe un accoppiamento
/// sbagliato.
/// </summary>
public struct AbilityThreatCue
{
    /// <summary>false = nessuna minaccia in corso: tutti i reattori tornano a riposo.</summary>
    public bool Active;

    public AbilityBase Ability;
    public IInteractableElement Caster;
    public IReadOnlyList<ITargettable> Targets;
    public AbilityIntent Intent;

    /// <summary><see cref="DamageType.None"/> se l'abilita' non e' offensiva o non sa dire il proprio elemento.</summary>
    public DamageType Element;

    public static AbilityThreatCue Begin(AbilityBase ability, IInteractableElement caster,
                                         IReadOnlyList<ITargettable> targets)
    {
        return new AbilityThreatCue
        {
            Active   = true,
            Ability  = ability,
            Caster   = caster,
            Targets  = targets,
            Intent   = ability.GetIntent(),
            Element  = ability is IOffensiveAbility offensive
                           ? offensive.ResolveDamageElement(caster)
                           : DamageType.None
        };
    }

    /// <summary>
    /// Chiusura globale, non per-bersaglio: regge perche' il turn loop e' sequenziale e Execution e' uno
    /// stato singolo, quindi non esistono due esecuzioni sovrapposte da distinguere.
    /// </summary>
    public static AbilityThreatCue End => default;
}
