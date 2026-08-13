using System.Collections.Generic;
using System.Threading;
using AYellowpaper.SerializedCollections;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Animazione di lifecycle per un personaggio (spawn, morte, ecc.), mirror di <see cref="HealthBehaviorSO"/>.
/// Le implementazioni devono restare stateless: tutto lo stato mutabile vive in <see cref="LifecycleAnimationContext"/>
/// e in <see cref="LifecycleVfxSession"/>, così un solo asset condiviso può essere referenziato da più prefab
/// senza Instantiate per-istanza.
///
/// L'animazione è espressa per SINGOLO bersaglio (<see cref="PrepareTarget"/> / <see cref="PlayTarget"/>) e
/// applicata a tutti i bersagli del contesto: il corpo e i satelliti registrati (parti di nemici compositi,
/// cappelli, armi), che sono fuori dalla gerarchia del pivot e altrimenti resterebbero immobili.
/// Lavorare per-bersaglio permette anche di agganciare un satellite registrato a fase già iniziata.
/// </summary>
public abstract class LifecycleAnimationSO : ScriptableObject
{
    [Header("VFX")]
    [SerializeField] private VfxCueEventChannel _vfxChannel;
    [SerializeField] private VfxStopEventChannel _vfxStopChannel;

    // protected e non private: il drawer del package risolve il campo con Type.GetField() sul tipo
    // dell'ASSET (una sottoclasse), e .NET non restituisce i campi private ereditati — il drawer
    // riceverebbe null e andrebbe in NullReferenceException a ogni repaint dell'Inspector.
    // I protected ereditati invece li trova. Nessuna sottoclasse deve leggerlo direttamente.
    [Tooltip("VFX della fase, raggruppati per momento interno. Gli spec dello stesso stage partono insieme.")]
    [SerializeField] protected SerializedDictionary<LifecycleVfxStage, List<LifecycleVfxSpec>> _vfx = new();

    /// <summary>
    /// Applica istantaneamente lo stato iniziale a tutti i bersagli (es. sprite già sott'acqua).
    /// Chiamata nello stesso frame in cui il GO viene attivato, prima del rendering.
    ///
    /// Puramente visiva: i VFX di <see cref="LifecycleVfxStage.Prepare"/> li alza il chiamante con
    /// <see cref="BeginVfx"/> subito dopo, così l'ordine "stato applicato → effetto" è garantito e la
    /// ri-preparazione di una fase già preparata non li fa ripartire.
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

    /// <summary>
    /// Apre la sessione VFX della fase e alza lo stage <see cref="LifecycleVfxStage.Prepare"/>.
    /// Chiamata una sola volta per fase, subito dopo <see cref="Prepare"/>.
    /// </summary>
    public void BeginVfx(LifecycleVfxSession session, in LifecycleAnimationContext ctx)
    {
        if (session == null) return;

        session.Begin(_vfxStopChannel);

        // Wiring dimenticato: un loop senza canale di stop resterebbe acceso per sempre. Un solo log per
        // fase (questo metodo gira una volta a fase, non per frame), niente stato da tracciare.
        if (_vfxStopChannel == null && HasLoopingSpec())
            Debug.LogWarning($"[{name}] VFX in loop senza _vfxStopChannel assegnato: non verranno mai fermati.", this);

        RaiseVfxStage(LifecycleVfxStage.Prepare, in ctx, session);
    }

    /// <summary>
    /// Riproduce la fase: movimento di tutti i bersagli, con i VFX degli stage
    /// <see cref="LifecycleVfxStage.Movement"/> ed <see cref="LifecycleVfxStage.End"/> ai loro appigli.
    ///
    /// I persistenti aperti qui e in <see cref="BeginVfx"/> NON vengono spenti da questo metodo: lo fa il
    /// <c>finally</c> del <see cref="CharacterLifecycleAnimator"/>, che copre anche la cancellazione del
    /// token (personaggio distrutto a metà emersione) e la fase preparata e mai giocata.
    ///
    /// Il <paramref name="token"/> non è consumato dal template ma resta in firma: è il contratto per una
    /// sottoclasse che avesse bisogno di attendere qualcosa, e l'animator lo propaga già.
    /// </summary>
    public virtual async Awaitable PlayAsync(
        LifecycleAnimationContext ctx, LifecycleVfxSession session, CancellationToken token)
    {
        RaiseVfxStage(LifecycleVfxStage.Movement, in ctx, session);
        await PlayAllTargetsAsync(ctx);
        RaiseVfxStage(LifecycleVfxStage.End, in ctx, session);
    }

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

    /// <summary>
    /// Alza tutti gli spec di uno stage e registra nella sessione gli handle dei persistenti.
    /// Stage assente dal dizionario = no-op silenzioso: è la configurazione voluta, non tutte le fasi
    /// hanno un VFX per ogni momento.
    /// </summary>
    protected void RaiseVfxStage(
        LifecycleVfxStage stage, in LifecycleAnimationContext ctx, LifecycleVfxSession session)
    {
        if (_vfx == null || !_vfx.TryGetValue(stage, out List<LifecycleVfxSpec> specs) || specs == null)
            return;

        for (int i = 0; i < specs.Count; i++)
        {
            LifecycleVfxSpec spec = specs[i];
            if (spec == null || !spec.IsConfigured) continue;

            session?.Track(spec.Raise(_vfxChannel, in ctx));
        }
    }

    private bool HasLoopingSpec()
    {
        if (_vfx == null) return false;

        foreach (KeyValuePair<LifecycleVfxStage, List<LifecycleVfxSpec>> entry in _vfx)
        {
            List<LifecycleVfxSpec> specs = entry.Value;
            if (specs == null) continue;

            for (int i = 0; i < specs.Count; i++)
                if (specs[i] != null && specs[i].IsConfigured && specs[i].Loop) return true;
        }

        return false;
    }
}
