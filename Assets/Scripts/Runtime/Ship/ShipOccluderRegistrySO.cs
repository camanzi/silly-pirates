using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elenco dei <see cref="ShipOccluderRegistry"/> attivi in scena. Sostituisce lo snapshot
/// <c>FindObjectsByType</c> che ShipOcclusionFader faceva in Awake: quello perdeva qualunque nave
/// abilitata dopo l'avvio (l'arrivo della nave nell'intro, o una scena caricata in additivo).
///
/// Multi-registrant, stessa forma di SpawnPointManagerSO.
/// </summary>
[CreateAssetMenu(fileName = "Ship Occluder Registry", menuName = "Anchors/Ship Occluder Registry")]
public class ShipOccluderRegistrySO : ScriptableObject
{
    private readonly List<ShipOccluderRegistry> _registries = new();

    public IReadOnlyList<ShipOccluderRegistry> Registries => _registries;

    /// <summary>Alzato a ogni registrazione, così i consumatori possono inizializzare i renderer che arrivano tardi.</summary>
    public event Action<ShipOccluderRegistry> OnRegistered;

    // Vedi il commento in RuntimeAnchorSO: rete di sicurezza per il domain reload, non il meccanismo
    // che regge le transizioni di scena.
    private void OnEnable() => _registries.Clear();

    public void Register(ShipOccluderRegistry registry)
    {
        if (registry == null || _registries.Contains(registry)) return;
        _registries.Add(registry);
        OnRegistered?.Invoke(registry);
    }

    public void Unregister(ShipOccluderRegistry registry) => _registries.Remove(registry);
}
