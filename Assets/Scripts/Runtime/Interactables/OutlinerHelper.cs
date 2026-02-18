using System.Collections.Generic;
using UnityEngine;

public class OutlinerHelper : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private RenderingLayerMask _outlineLayer;

    private Renderer[] _objectRenderers;

    void OnEnable()
    {
        _objectRenderers = GetComponentsInChildren<Renderer>();
    }

    public void AddToOutline()
    {
        uint maskToAdd = _outlineLayer.value;   
        foreach (Renderer renderer in _objectRenderers)
        {
            renderer.renderingLayerMask |= maskToAdd;
        }
    }

    public void RemoveFromOutline()
    {
        uint maskToRemove = _outlineLayer.value;
        foreach (Renderer renderer in _objectRenderers)
        {
            renderer.renderingLayerMask &= ~maskToRemove;
        }
    }
}
