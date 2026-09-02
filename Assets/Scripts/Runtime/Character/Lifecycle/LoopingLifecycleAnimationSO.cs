using System.Collections.Generic;
using PrimeTween;

/// <summary>
/// Contratto per una <see cref="LifecycleAnimationSO"/> che non finisce da sola: gira finché nessuno la
/// ferma esplicitamente (<see cref="CharacterLifecycleAnimator.StopLoop"/>). Non aggiunge dati — le
/// sottoclassi restano quello che sono già, solo <see cref="LifecycleAnimationSO.PrepareTarget"/> e
/// <see cref="LifecycleAnimationSO.PlayTarget"/> — aggiunge solo il binario di avvio che bypassa
/// <see cref="LifecycleAnimationSO.PlayAsync"/>.
///
/// Il bypass è strutturale, non stilistico: un <see cref="PlayTarget"/> a <c>cycles: -1</c> non torna mai
/// da un <c>await</c>. <c>PlayAsync</c> della base non va MAI chiamata su un'istanza di questo tipo — la
/// guardia che lo impedisce sta in <see cref="CharacterLifecycleAnimator.PlayAsync"/>, non qui, perché è
/// l'animator (e non la SO) a decidere quale binario usare.
/// </summary>
public abstract class LoopingLifecycleAnimationSO : LifecycleAnimationSO
{
    /// <summary>
    /// Avvia il loop su TUTTI i bersagli senza attendere nessuno: gemello non-attendente di
    /// <c>PlayAllTargetsAsync</c>. Un tween infinito non ha una tween "portante" da aspettare, quindi qui
    /// non serve la distinzione corpo/satelliti del binario delle transizioni.
    /// </summary>
    public void StartLoops(in LifecycleAnimationContext ctx, LifecycleTweenSession session)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        for (int i = 0; i < targets.Count; i++)
            session.Track(PlayTarget(targets[i]));
    }

    /// <summary>Avvia il loop sul singolo bersaglio: usato per il satellite che si registra a loop già partito.</summary>
    public Tween StartLoopOn(in LifecycleAnimationTarget target) => PlayTarget(target);

    /// <summary>
    /// Alza gli spec di <see cref="LifecycleVfxStage.Movement"/>. Wrapper pubblico perché <c>RaiseVfxStage</c>
    /// è <c>protected</c> sulla base (serve al drawer del dizionario VFX, vedi commento lì) e l'animator
    /// deve poterlo chiamare dal binario loop esattamente come fa <see cref="LifecycleAnimationSO.PlayAsync"/>
    /// per quello delle transizioni.
    /// </summary>
    public void RaiseMovementVfx(in LifecycleAnimationContext ctx, LifecycleVfxSession session)
        => RaiseVfxStage(LifecycleVfxStage.Movement, in ctx, session);

    /// <summary>
    /// Alza gli spec di <see cref="LifecycleVfxStage.End"/>. Per un'animazione che non finisce da sola,
    /// "a movimento concluso" non ha senso: qui significa "nel momento in cui il loop viene spento".
    /// </summary>
    public void RaiseEndVfx(in LifecycleAnimationContext ctx, LifecycleVfxSession session)
        => RaiseVfxStage(LifecycleVfxStage.End, in ctx, session);
}
