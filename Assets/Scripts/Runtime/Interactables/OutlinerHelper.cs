using System.Collections.Generic;
using UnityEngine;

public class OutlinerHelper : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private OutlineLayerConfigSO _layerConfig;

    private Renderer[] _objectRenderers;
    private uint _currentMask;

    void OnEnable()
    {
        _objectRenderers = GetComponentsInChildren<Renderer>();
    }

    public void SetOutline(OutlineState state)
    {
        uint newMask = _layerConfig.GetLayer(state).value;
        if (newMask == _currentMask) return;

        foreach (Renderer renderer in _objectRenderers)
        {
            renderer.renderingLayerMask &= ~_currentMask;
            renderer.renderingLayerMask |= newMask;
        }

        _currentMask = newMask;
    }

    public void ClearOutline()
    {
        foreach (Renderer renderer in _objectRenderers)
        {
            renderer.renderingLayerMask &= ~_currentMask;
        }

        _currentMask = 0;
    }
}
