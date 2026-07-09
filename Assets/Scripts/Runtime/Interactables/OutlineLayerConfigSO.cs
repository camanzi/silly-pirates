using UnityEngine;

[CreateAssetMenu(fileName = "OutlineLayerConfig", menuName = "Interactables/Outline Layer Config")]
public class OutlineLayerConfigSO : ScriptableObject
{
    [SerializeField] private RenderingLayerMask _defaultLayer;
    [SerializeField] private RenderingLayerMask _validTargetLayer;
    [SerializeField] private RenderingLayerMask _invalidTargetLayer;

    public RenderingLayerMask GetLayer(OutlineState state)
    {
        switch (state)
        {
            case OutlineState.ValidTarget:
                return _validTargetLayer;
            case OutlineState.InvalidTarget:
                return _invalidTargetLayer;
            case OutlineState.Default:
            default:
                return _defaultLayer;
        }
    }
}
