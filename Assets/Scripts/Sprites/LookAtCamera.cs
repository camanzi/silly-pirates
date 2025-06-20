using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        Vector3 cameraPosition = _camera.transform.position;
        cameraPosition.y = transform.position.y;
        transform.LookAt(cameraPosition);
        transform.Rotate(0, 180f, 0);
    }

}
