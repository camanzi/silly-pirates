using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Assigns a CinemachineCamera's Tracking/LookAt targets from a <see cref="TransformAnchorSO"/>.
///
/// A component is needed because <c>CinemachineCamera.Target</c> is a Cinemachine struct: its fields
/// cannot be assigned from a ScriptableObject, so the only way a vcam inside a prefab could point at an
/// object in another prefab was a scene override.
///
/// It also reassigns when the anchor changes: the target may register after the camera.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineTargetBinder : MonoBehaviour
{
    [SerializeField] private TransformAnchorSO _trackingTargetAnchor;
    [SerializeField] private TransformAnchorSO _lookAtTargetAnchor;

    private CinemachineCamera _camera;

    private void OnEnable()
    {
        if (_camera == null) _camera = GetComponent<CinemachineCamera>();

        if (_trackingTargetAnchor != null)
        {
            ApplyTrackingTarget(_trackingTargetAnchor.Value);
            _trackingTargetAnchor.OnValueChanged += ApplyTrackingTarget;
        }
        if (_lookAtTargetAnchor != null)
        {
            ApplyLookAtTarget(_lookAtTargetAnchor.Value);
            _lookAtTargetAnchor.OnValueChanged += ApplyLookAtTarget;
        }
    }

    private void OnDisable()
    {
        if (_trackingTargetAnchor != null) _trackingTargetAnchor.OnValueChanged -= ApplyTrackingTarget;
        if (_lookAtTargetAnchor != null) _lookAtTargetAnchor.OnValueChanged -= ApplyLookAtTarget;
    }

    private void ApplyTrackingTarget(Transform target)
    {
        if (_camera == null) return;
        _camera.Target.TrackingTarget = target;
    }

    private void ApplyLookAtTarget(Transform target)
    {
        if (_camera == null) return;
        // CustomLookAtTarget is deliberately left alone: it is the flag deciding whether LookAtTarget
        // actually counts or whether the aim falls back to TrackingTarget. It stays as authored on the
        // prefab, so the behaviour is identical to the scene overrides being replaced here.
        _camera.Target.LookAtTarget = target;
    }
}
