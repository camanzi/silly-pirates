using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    private Camera _camera;

    private void OnEnable()
    {
        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(LookAtCamera)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
            return;
        }

        _camera = _mainCameraAnchor.Value;
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera) => _camera = camera;

    private void LateUpdate()
    {
        if (_camera == null) return;

        Vector3 cameraPosition = _camera.transform.position;
        cameraPosition.y = transform.position.y;
        transform.LookAt(cameraPosition);
        transform.Rotate(0, 180f, 0);
    }
}
