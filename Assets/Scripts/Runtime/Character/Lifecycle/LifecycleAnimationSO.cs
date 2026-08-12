using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Animazione di lifecycle per un personaggio (spawn, morte, ecc.), mirror di <see cref="HealthBehaviorSO"/>.
/// Le implementazioni devono restare stateless: tutto lo stato mutabile vive in <see cref="LifecycleAnimationContext"/>,
/// così un solo asset condiviso può essere referenziato da più prefab senza Instantiate per-istanza.
///
/// L'animazione è espressa per SINGOLO bersaglio (<see cref="PrepareTarget"/> / <see cref="PlayTarget"/>) e
/// applicata a tutti i bersagli del contesto: il corpo e i satelliti registrati (parti di nemici compositi,
/// cappelli, armi), che sono fuori dalla gerarchia del pivot e altrimenti resterebbero immobili.
/// Lavorare per-bersaglio permette anche di agganciare un satellite registrato a fase già iniziata.
/// </summary>
public abstract class LifecycleAnimationSO : ScriptableObject
{
    /// <summary>
    /// Applica istantaneamente lo stato iniziale a tutti i bersagli (es. sprite già sott'acqua).
    /// Chiamata nello stesso frame in cui il GO viene attivato, prima del rendering.
    /// </summary>
    public virtual void Prepare(in LifecycleAnimationContext ctx)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        for (int i = 0; i < targets.Count; i++)
            PrepareTarget(targets[i]);
    }

    /// <summary>Stato iniziale di un singolo bersaglio.</summary>
    public virtual void PrepareTarget(in LifecycleAnimationTarget target) { }

    /// <summary>
    /// Avvia le tween su un singolo bersaglio e restituisce quella "portante" (il movimento), l'unica da
    /// attendere. <c>default</c> significa "niente da attendere".
    /// </summary>
    public virtual Tween PlayTarget(in LifecycleAnimationTarget target) => default;

    public abstract Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token);

    /// <summary>
    /// Avvia <see cref="PlayTarget"/> su tutti i bersagli e attende solo quella del corpo: hanno tutti la
    /// stessa durata, quindi finiscono insieme e non serve una <c>Sequence</c>.
    /// </summary>
    protected async Awaitable PlayAllTargetsAsync(LifecycleAnimationContext ctx)
    {
        IReadOnlyList<LifecycleAnimationTarget> targets = ctx.Targets;
        if (targets.Count == 0) return;

        for (int i = 1; i < targets.Count; i++)
            _ = PlayTarget(targets[i]);   // i satelliti accompagnano il corpo: nessuno li attende

        Tween bodyTween = PlayTarget(targets[0]);
        if (bodyTween.isAlive) await bodyTween;
    }
}
