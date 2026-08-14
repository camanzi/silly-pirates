using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caches the renderers under this GameObject that belong to the "ShipOccluder" layer —
/// tall structural ship parts (masts, sails, rigging, crow's nest) that may need to be
/// faded out when they block the action camera's view of the current subject.
/// Consumed by the camera-side occlusion fader (ShipOcclusionFader).
/// </summary>
public class ShipOccluderRegistry : MonoBehaviour
{
    [SerializeField] private ShipOccluderRegistrySO _registry;

    private readonly List<Renderer> _occluderRenderers = new();

    public IReadOnlyList<Renderer> OccluderRenderers => _occluderRenderers;

    private void OnEnable()
    {
        _occluderRenderers.Clear();

        int occluderLayer = LayerMask.NameToLayer("ShipOccluder");
        if (occluderLayer < 0)
        {
            Debug.LogWarning("ShipOccluderRegistry: 'ShipOccluder' layer not found. No renderers will be tracked.", this);
            return;
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.gameObject.layer == occluderLayer)
            {
                _occluderRenderers.Add(renderer);
            }
        }

        // Registrazione DOPO aver popolato la lista: il fader inizializza i renderer appena viene
        // notificato, e li vuole trovare già pronti.
        if (_registry != null) _registry.Register(this);
    }

    private void OnDisable()
    {
        if (_registry != null) _registry.Unregister(this);
    }
}
