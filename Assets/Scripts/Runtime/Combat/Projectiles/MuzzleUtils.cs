using UnityEngine;

/// <summary>
/// The single place a caster's firing origin is resolved.
/// Abilities are ScriptableObjects and cannot reference scene transforms:
/// the command resolves it here, at creation time, starting from the caster.
/// </summary>
public static class MuzzleUtils
{
    /// <summary>
    /// Order: explicit override (<see cref="IMuzzleOwner"/>) -> a <see cref="MuzzleAnchor"/> marker in
    /// the hierarchy -> the caster's root.
    /// </summary>
    public static Transform Resolve(IInteractableElement caster)
    {
        if (caster == null || caster.Transform == null) return null;

        if (caster is IMuzzleOwner owner && owner.Muzzle != null) return owner.Muzzle;

        var marker = caster.Transform.GetComponentInChildren<MuzzleAnchor>(true);
        return marker != null ? marker.transform : caster.Transform;
    }
}
