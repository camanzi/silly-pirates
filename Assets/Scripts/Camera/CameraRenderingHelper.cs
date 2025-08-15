using UnityEngine;

public class CameraRenderingHelper : MonoBehaviour
{
    [Header("Culling mask settings")]
    [SerializeField] private LayerMask defaultMask;
    [SerializeField] private LayerMask tacticalMask;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _camera.cullingMask = defaultMask;
    }

    public void OnEnableTacticalView(bool enable) 
    {
        _camera.cullingMask = enable ? tacticalMask : defaultMask;
    }
}
