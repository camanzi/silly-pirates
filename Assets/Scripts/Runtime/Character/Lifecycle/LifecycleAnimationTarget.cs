using PrimeTween;
using UnityEngine;

/// <summary>
/// Un singolo bersaglio visivo animato dal lifecycle: il pivot del corpo (es. <c>MeshHolder</c>) oppure un
/// "satellite" — una parte di nemico composito, un cappello, un'arma — che semanticamente fa parte dello
/// stesso corpo ma vive FUORI dalla gerarchia del pivot, quindi non ne eredita le animazioni.
///
/// La posa a riposo viene catturata al momento della registrazione e non è più aggiornata: è il
/// riferimento da cui ogni animazione parte e a cui ogni reset torna.
/// </summary>
public readonly struct LifecycleAnimationTarget
{
    public readonly Transform Pivot;
    public readonly SpriteRenderer Renderer;   // può essere null: un satellite senza sprite si limita a muoversi
    public readonly Vector3 RestLocalPosition;
    public readonly Vector3 RestLocalScale;
    public readonly Color RestColor;

    public bool IsValid => Pivot != null;

    public LifecycleAnimationTarget(
        Transform pivot,
        SpriteRenderer renderer,
        Vector3 restLocalPosition,
        Vector3 restLocalScale,
        Color restColor)
    {
        Pivot = pivot;
        Renderer = renderer;
        RestLocalPosition = restLocalPosition;
        RestLocalScale = restLocalScale;
        RestColor = restColor;
    }

    /// <summary>Cattura la posa corrente di <paramref name="pivot"/> come posa a riposo.</summary>
    public static LifecycleAnimationTarget Capture(Transform pivot, SpriteRenderer renderer)
    {
        Color restColor = renderer != null ? renderer.color : Color.white;
        return new LifecycleAnimationTarget(
            pivot, renderer, pivot.localPosition, pivot.localScale, restColor);
    }

    /// <summary>Ripristina la sola posa del transform. Sicuro su oggetti già distrutti.</summary>
    public void ResetPoseToRest()
    {
        if (Pivot == null) return;
        Pivot.localPosition = RestLocalPosition;
        Pivot.localScale = RestLocalScale;
    }

    /// <summary>Ripristina posa e colore pieno (RGBA): usato dai revive, dove il tint "morto" va rimosso.</summary>
    public void ResetToRest()
    {
        ResetPoseToRest();
        if (Renderer != null) Renderer.color = RestColor;
    }

    /// <summary>
    /// Scrive il solo canale alpha preservando l'RGB: il tint applicato da
    /// <see cref="DirectionalSpriteController.SetDeadVisual"/> a una parte rotta deve sopravvivere al fade
    /// di lifecycle, altrimenti una parte distrutta tornerebbe del colore di quella viva mentre affonda.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (Renderer == null) return;
        Color color = Renderer.color;
        color.a = alpha;
        Renderer.color = color;
    }

    /// <summary>
    /// Ferma i tween che altri sistemi stanno già facendo girare su questo bersaglio (lo shake infinito di
    /// un telegraph, un salto interrotto a metà, un fade precedente). Senza questo continuerebbero a
    /// scrivere sullo stesso <c>localPosition</c>/colore che il lifecycle sta animando.
    /// </summary>
    public void StopActiveTweens()
    {
        if (Pivot != null) Tween.StopAll(Pivot);
        if (Renderer != null) Tween.StopAll(Renderer);
    }
}
