using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A reference to a scene instance resolved at runtime, so that consumer prefabs need not hold
/// cross-object references — which cannot be assigned inside a prefab — and therefore stay usable in
/// any scene with no manual rewiring.
///
/// THE CONTRACT FOR CONSUMERS — <i>pull-then-subscribe</i>, always in OnEnable:
/// <code>
/// private void OnEnable()
/// {
///     HandleAnchorChanged(_anchor.Value);             // whatever is registered ALREADY
///     _anchor.OnValueChanged += HandleAnchorChanged;  // whatever arrives LATER
/// }
/// private void OnDisable() => _anchor.OnValueChanged -= HandleAnchorChanged;
/// </code>
/// The pull covers "the producer has already registered" (same scene, or an additive scene loaded
/// earlier); the subscribe covers "the producer arrives later" (consumer in the persistent scene, rig in
/// the combat one). A single pattern covers both orderings: do not add lazy resolution at the call
/// sites.
///
/// Registrants are kept in a list and the most recent one wins. That is what the additive swap needs:
/// the new scene is loaded (its producer registers) and only afterwards is the old one unloaded (its
/// Unregister becomes a no-op on the value). Within that window two producers are legitimately alive at
/// once, and Value must never pass through null.
/// </summary>
public abstract class RuntimeAnchorSO<T> : ScriptableObject where T : Component
{
    private readonly List<T> _registrants = new();

    /// <summary>The most recent live registrant, or null when there are none.</summary>
    public T Value
    {
        get
        {
            for (int i = _registrants.Count - 1; i >= 0; i--)
                if (_registrants[i] != null)
                    return _registrants[i];
            return null;
        }
    }

    public bool IsSet => Value != null;

    public event Action<T> OnValueChanged;

    // This is NOT load-bearing for scene transitions. Like SpawnPointManagerSO.OnEnable, it fires once
    // per load of the asset into memory (app start / domain reload), not on every scene load. Correctness
    // across repeated loads comes entirely from the Register/Unregister protocol: do not "simplify" this
    // line believing it is what holds the transitions together.
    private void OnEnable() => _registrants.Clear();

    public void Register(T instance)
    {
        if (instance == null || _registrants.Contains(instance)) return;

        WarnIfDuplicateInSameScene(instance);

        T previous = Value;
        _registrants.Add(instance);
        if (Value != previous) OnValueChanged?.Invoke(Value);
    }

    public void Unregister(T instance)
    {
        T previous = Value;
        if (!_registrants.Remove(instance)) return;
        if (Value != previous) OnValueChanged?.Invoke(Value);
    }

    /// <summary>
    /// Two producers in the SAME scene are always an authoring mistake (e.g. two PF_ActionCamera left
    /// active). Two producers in DIFFERENT scenes, on the other hand, are the normal overlap window of an
    /// additive swap and must not be flagged: a warning there would be noise on every transition.
    /// </summary>
    private void WarnIfDuplicateInSameScene(T incoming)
    {
        for (int i = 0; i < _registrants.Count; i++)
        {
            T existing = _registrants[i];
            if (existing == null) continue;
            if (existing.gameObject.scene != incoming.gameObject.scene) continue;

            Debug.LogWarning(
                $"[{name}] '{incoming.name}' and '{existing.name}' are registered on the same anchor " +
                $"in the same scene ('{incoming.gameObject.scene.name}'). The invariant is a single " +
                $"active producer: the most recent one wins.", incoming);
            return;
        }
    }
}
