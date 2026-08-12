using UnityEngine;

/// <summary>
/// Punto unico di risoluzione dell'origine di tiro di un caster.
/// Le abilita' sono ScriptableObject e non possono referenziare transform di scena:
/// il comando risolve qui, alla creazione, partendo dal caster.
/// </summary>
public static class MuzzleUtils
{
    /// <summary>
    /// Ordine: override esplicito (<see cref="IMuzzleOwner"/>) -> marker
    /// <see cref="MuzzleAnchor"/> in hierarchy -> root del caster.
    /// </summary>
    public static Transform Resolve(IInteractableElement caster)
    {
        if (caster == null || caster.Transform == null) return null;

        if (caster is IMuzzleOwner owner && owner.Muzzle != null) return owner.Muzzle;

        var marker = caster.Transform.GetComponentInChildren<MuzzleAnchor>(true);
        return marker != null ? marker.transform : caster.Transform;
    }
}
