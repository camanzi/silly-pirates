using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Esegue una sequenza di salto squash&amp;stretch su un pivot visivo (tipicamente il MeshHolder),
/// mai sul transform di griglia: collider e occupancy restano fermi.
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
    /// Al termine il pivot è fermo in posizione di apice, pronto per l'hang time del chiamante.
    /// </summary>
    public static async Awaitable JumpUpAsync(
        Transform animationRoot,
        Vector3 restLocalPosition,
        Vector3 restLocalScale,
        float apexWorldHeight,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        // L'altezza arriva in unità mondo (derivata dalla Y del target), ma il tween muove una
        // localPosition: se il parent è scalato le due non coincidono.
        float parentScaleY = animationRoot.parent != null ? animationRoot.parent.lossyScale.y : 1f;
        float apexLocalHeight = Mathf.Approximately(parentScaleY, 0f) ? apexWorldHeight : apexWorldHeight / parentScaleY;
        Vector3 apexLocalPosition = restLocalPosition + Vector3.up * apexLocalHeight;

        // 1) Anticipazione: si schiaccia decelerando, "carica" l'energia prima dello scatto.
        await Tween.Scale(animationRoot, ScaleOf(restLocalScale, config.AnticipationScale),
                          config.AnticipationDuration, config.AnticipationEase);
        token.ThrowIfCancellationRequested();

        // 2) Stacco: spruzzo d'acqua + stiramento verticale secco, in parallelo alla salita.
        //    La salita decelera (RiseEase) come un corpo che perde velocità sotto gravità.
        SpawnSplash(config.SplashVfxPrefab, splashWorldPosition);
        _ = Tween.Scale(animationRoot, ScaleOf(restLocalScale, config.LaunchScale),
                        config.LaunchDuration, config.LaunchEase);
        await Tween.LocalPosition(animationRoot, apexLocalPosition, config.RiseDuration, config.RiseEase);
        token.ThrowIfCancellationRequested();

        // 3) Apice: la stiratura si riassorbe quasi del tutto, altrimenti in sospensione leggerebbe
        //    come gommoso invece che "in bilico".
        await Tween.Scale(animationRoot, ScaleOf(restLocalScale, config.ApexScale),
                          config.ApexSettleDuration, Ease.OutQuad);
    }

    /// <summary>
    /// Caduta dall'apice, impatto con splash e recupero elastico. Ripristina SEMPRE posizione e scala
    /// di riposo, anche in caso di eccezione o cancellazione.
    /// </summary>
    public static async Awaitable FallDownAsync(
        Transform animationRoot,
        Vector3 restLocalPosition,
        Vector3 restLocalScale,
        JumpAnimationConfigSO config,
        Vector3 splashWorldPosition,
        CancellationToken token = default)
    {
        try
        {
            // 4) Caduta: accelera (FallEase), con leggero stiramento residuo mentre precipita.
            _ = Tween.Scale(animationRoot, ScaleOf(restLocalScale, config.FallScale),
                            config.FallDuration * 0.6f, Ease.InQuad);
            await Tween.LocalPosition(animationRoot, restLocalPosition, config.FallDuration, config.FallEase);
            token.ThrowIfCancellationRequested();

            // 5) Impatto in acqua: spruzzo + schiacciamento, più marcato dell'anticipazione perché
            //    l'energia da dissipare all'arrivo è maggiore di quella accumulata alla partenza.
            SpawnSplash(config.SplashVfxPrefab, splashWorldPosition);
            await Tween.Scale(animationRoot, ScaleOf(restLocalScale, config.LandingScale),
                              config.LandingSquashDuration, config.LandingSquashEase);
            token.ThrowIfCancellationRequested();

            // 6) Follow-through elastico: rende l'impatto assorbito e non troncato.
            await Tween.Scale(animationRoot, restLocalScale,
                              config.LandingRecoveryDuration, config.LandingRecoveryEase);
        }
        finally
        {
            ResetToRest(animationRoot, restLocalPosition, restLocalScale);
        }
    }

    /// <summary>
    /// Riporta il pivot alla posa di riposo. Sicuro su oggetti già distrutti (il confronto con null
    /// di Unity li intercetta). Da chiamare anche dal chiamante, per coprire i fallimenti che
    /// avvengono prima di <see cref="FallDownAsync"/>.
    /// </summary>
    public static void ResetToRest(Transform animationRoot, Vector3 restLocalPosition, Vector3 restLocalScale)
    {
        if (animationRoot == null) return;
        animationRoot.localPosition = restLocalPosition;
        animationRoot.localScale = restLocalScale;
    }

    private static Vector3 ScaleOf(Vector3 restScale, JumpAnimationConfigSO.SquashStretchScale s)
        => new(restScale.x * s.Horizontal, restScale.y * s.Vertical, restScale.z * s.Horizontal);

    private static void SpawnSplash(VFXController prefab, Vector3 worldPosition)
    {
        if (prefab == null) return;     // splash non configurato: è una scelta, non un errore
        Object.Instantiate(prefab, worldPosition, Quaternion.identity);
    }
}
