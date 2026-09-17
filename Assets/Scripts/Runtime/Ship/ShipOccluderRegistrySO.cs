using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The list of <see cref="ShipOccluderRegistry"/> instances active in the scene. It replaces the
/// <c>FindObjectsByType</c> snapshot ShipOcclusionFader used to take in Awake: that one missed any ship
/// enabled after start-up (the ship arriving in the intro, or an additively loaded scene).
///
/// Multi-registrant, in the same shape as SpawnPointManagerSO.
/// </summary>
[CreateAssetMenu(fileName = "Ship Occluder Registry", menuName = "Anchors/Ship Occluder Registry")]
public class ShipOccluderRegistrySO : ScriptableObject
{
    private readonly List<ShipOccluderRegistry> _registries = new();

    public IReadOnlyList<ShipOccluderRegistry> Registries => _registries;

    /// <summary>Raised on every registration, so consumers can initialize renderers that arrive late.</summary>
    public event Action<ShipOccluderRegistry> OnRegistered;

    // See the comment in RuntimeAnchorSO: a safety net for domain reloads, not the mechanism that holds
    // scene transitions together.
    private void OnEnable() => _registries.Clear();

    public void Register(ShipOccluderRegistry registry)
    {
        if (registry == null || _registries.Contains(registry)) return;
        _registries.Add(registry);
        OnRegistered?.Invoke(registry);
    }

    public void Unregister(ShipOccluderRegistry registry) => _registries.Remove(registry);
}
