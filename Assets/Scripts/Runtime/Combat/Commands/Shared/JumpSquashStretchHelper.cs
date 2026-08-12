using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Esegue una sequenza di salto squash&amp;stretch sui pivot visivi di un personaggio (tipicamente il
/// MeshHolder più i suoi satelliti, es. le parti di un nemico composito), mai sul transform di griglia:
/// collider e occupancy restano fermi.
///
/// Tutti i bersagli si muovono insieme con la stessa durata; viene attesa solo la tween del corpo
/// (<c>targets[0]</c>), che le altre accompagnano — così scettro e cappello saltano col loro proprietario
/// invece di restare a terra.
///
/// Spezzata in due metà (<see cref="JumpUpAsync"/> / <see cref="FallDownAsync"/>) invece di essere
/// un unico metodo con callback, così il chiamante può inserire un'azione qualunque — sparare un
/// proiettile, alzare una cue camera — mentre il personaggio è sospeso all'apice, con un semplice
/// await in sequenza e senza indirection via delegati.
/// </summary>
public static class JumpSquashStretchHelper
{
    /// <summary>
    /// Anticipazione, stacco (con splash) e salita fino all'apice, con assestamento finale.
    /// Al termine i pivot sono fermi in posizione di apice, pronti per l'hang time del chiamante.
    /// </summary>
    public static async Awaitable JumpUpAsync(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        float apexWorldHeight,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        // L'altezza arriva in unità mondo (derivata dalla Y del target), ma i tween muovono una
        // localPosition: se il parent è scalato le due non coincidono. I satelliti condividono il parent
        // del corpo, quindi il fattore è lo stesso per tutti.
        Transform bodyPivot = targets[0].Pivot;
        float parentScaleY = bodyPivot.parent != null ? bodyPivot.parent.lossyScale.y : 1f;
        float apexLocalHeight = Mathf.Approximately(parentScaleY, 0f) ? apexWorldHeight : apexWorldHeight / parentScaleY;

        // 1) Anticipazione: si schiaccia decelerando, "carica" l'energia prima dello scatto.
        await ScaleAll(targets, config.AnticipationScale, config.AnticipationDuration, config.AnticipationEase);
        token.ThrowIfCancellationRequested();

        // 2) Stacco: spruzzo d'acqua + stiramento verticale secco, in parallelo alla salita.
        //    La salita decelera (RiseEase) come un corpo che perde velocità sotto gravità.
        SpawnSplash(config, splashWorldPosition);
        _ = ScaleAll(targets, config.LaunchScale, config.LaunchDuration, config.LaunchEase);
        await MoveAll(targets, apexLocalHeight, config.RiseDuration, config.RiseEase);
        token.ThrowIfCancellationRequested();

        // 3) Apice: la stiratura si riassorbe quasi del tutto, altrimenti in sospensione leggerebbe
        //    come gommoso invece che "in bilico".
        await ScaleAll(targets, config.ApexScale, config.ApexSettleDuration, Ease.OutQuad);
    }

    /// <summary>
    /// Caduta dall'apice, impatto con splash e recupero elastico. Ripristina SEMPRE posizione e scala
    /// di riposo, anche in caso di eccezione o cancellazione.
    /// </summary>
    public static async Awaitable FallDownAsync(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        try
        {
            // 4) Caduta: accelera (FallEase), con leggero stiramento residuo mentre precipita.
            _ = ScaleAll(targets, config.FallScale, config.FallDuration * 0.6f, Ease.InQuad);
            await MoveAll(targets, 0f, config.FallDuration, config.FallEase);
            token.ThrowIfCancellationRequested();

            // 5) Impatto in acqua: spruzzo + schiacciamento, più marcato dell'anticipazione perché
            //    l'energia da dissipare all'arrivo è maggiore di quella accumulata alla partenza.
            SpawnSplash(config, splashWorldPosition);
            await ScaleAll(targets, config.LandingScale, config.LandingSquashDuration, config.LandingSquashEase);
            token.ThrowIfCancellationRequested();

            // 6) Follow-through elastico: rende l'impatto assorbito e non troncato.
            await ScaleAllToRest(targets, config.LandingRecoveryDuration, config.LandingRecoveryEase);
        }
        finally
        {
            ResetToRest(targets);
        }
    }

    /// <summary>
    /// Riporta i pivot alla posa di riposo. Sicuro su oggetti già distrutti (il confronto con null
    /// di Unity li intercetta). Da chiamare anche dal chiamante, per coprire i fallimenti che
    /// avvengono prima di <see cref="FallDownAsync"/>.
    /// </summary>
    public static void ResetToRest(IReadOnlyList<LifecycleAnimationTarget> targets)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Count; i++)
            targets[i].ResetPoseToRest();
    }

    /// <summary>
    /// Avvia la scalata su tutti i bersagli e restituisce quella del corpo, l'unica da attendere:
    /// stessa durata per tutti, quindi finiscono insieme.
    /// </summary>
    private static Tween ScaleAll(
        IReadOnlyList<LifecycleAnimationTarget> targets,
        JumpAnimationConfigSO.SquashStretchScale scale,
        float duration,
        Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.Scale(target.Pivot, ScaleOf(target.RestLocalScale, scale), duration, ease);
        }

        var body = targets[0];
        return Tween.Scale(body.Pivot, ScaleOf(body.RestLocalScale, scale), duration, ease);
    }

    private static Tween ScaleAllToRest(
        IReadOnlyList<LifecycleAnimationTarget> targets, float duration, Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.Scale(target.Pivot, target.RestLocalScale, duration, ease);
        }

        var body = targets[0];
        return Tween.Scale(body.Pivot, body.RestLocalScale, duration, ease);
    }

    /// <summary>Solleva tutti i bersagli di <paramref name="localHeight"/> sopra la rispettiva posa a riposo.</summary>
    private static Tween MoveAll(
        IReadOnlyList<LifecycleAnimationTarget> targets, float localHeight, float duration, Ease ease)
    {
        for (int i = 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.Pivot == null) continue;
            _ = Tween.LocalPosition(
                target.Pivot, target.RestLocalPosition + Vector3.up * localHeight, duration, ease);
        }

        var body = targets[0];
        return Tween.LocalPosition(
            body.Pivot, body.RestLocalPosition + Vector3.up * localHeight, duration, ease);
    }

    private static Vector3 ScaleOf(Vector3 restScale, JumpAnimationConfigSO.SquashStretchScale s)
        => new(restScale.x * s.Horizontal, restScale.y * s.Vertical, restScale.z * s.Horizontal);

    private static void SpawnSplash(JumpAnimationConfigSO config, Vector3 worldPosition)
    {
        // splash non configurato: è una scelta, non un errore
        if (config.SplashVfxPrefab == null || config.VfxChannel == null) return;
        config.VfxChannel.RaiseEvent(VfxCue.At(config.SplashVfxPrefab, worldPosition));
    }
}
