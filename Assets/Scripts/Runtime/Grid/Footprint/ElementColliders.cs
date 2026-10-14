using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single answer to "which colliders make up this object": every enabled, non-trigger collider on an
/// active GameObject under the element root, the root included. The walk stops at any descendant that is an
/// object of its own (a nested <see cref="GridElement"/> or <see cref="IClickable"/>), so a child element's
/// collider never leaks into its parent's footprint or bounds. No marker component needed: the grouping is
/// the hierarchy itself.
/// </summary>
public static class ElementColliders
{
    // Scratch buffer for GetComponents. Main-thread only, like the rest of the grid code.
    private static readonly List<Collider> ComponentBuffer = new List<Collider>(4);

    /// <summary>Clears <paramref name="results"/> and fills it with the colliders of the object rooted at <paramref name="root"/>.</summary>
    public static void Collect(Transform root, List<Collider> results)
    {
        results.Clear();
        if (root == null) return;
        Visit(root, results);
    }

    /// <summary>
    /// The world-space union of the AABBs of <paramref name="colliders"/>. False when the list is empty.
    /// Reads <see cref="Collider.bounds"/>: call <see cref="Physics.SyncTransforms"/> first when transforms may
    /// have moved since the last simulation step.
    /// </summary>
    public static bool TryGetBounds(List<Collider> colliders, out Bounds bounds)
    {
        bounds = default;
        if (colliders == null || colliders.Count == 0) return false;

        bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Count; i++)
            bounds.Encapsulate(colliders[i].bounds);
        return true;
    }

    private static void Visit(Transform node, List<Collider> results)
    {
        if (!node.gameObject.activeInHierarchy) return;

        node.GetComponents(ComponentBuffer);
        for (int i = 0; i < ComponentBuffer.Count; i++)
        {
            Collider collider = ComponentBuffer[i];
            if (collider.enabled && !collider.isTrigger) results.Add(collider);
        }
        ComponentBuffer.Clear();

        for (int i = 0; i < node.childCount; i++)
        {
            Transform child = node.GetChild(i);
            if (IsSeparateObject(child)) continue;
            Visit(child, results);
        }
    }

    private static bool IsSeparateObject(Transform node)
        => node.TryGetComponent(out GridElement _) || node.TryGetComponent(out IClickable _);
}
