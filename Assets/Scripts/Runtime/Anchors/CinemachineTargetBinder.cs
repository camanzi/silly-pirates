using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Assegna Tracking/LookAt target di una CinemachineCamera da <see cref="TransformAnchorSO"/>.
///
/// Serve un componente perché <c>CinemachineCamera.Target</c> è una struct Cinemachine: i suoi campi
/// non sono assegnabili da un ScriptableObject, ed erano quindi l'unico modo per cui una vcab in
/// prefab potesse puntare a un oggetto di un altro prefab — cioè un override di scena.
///
/// Riassegna anche su cambio anchor: il bersaglio può registrarsi dopo la camera.
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
        // CustomLookAtTarget non viene toccato di proposito: è il flag che decide se LookAtTarget
        // conta davvero o se l'aim ricade su TrackingTarget. Resta com'è autorato sul prefab, così
        // il comportamento è identico a quello degli override di scena che stiamo sostituendo.
        _camera.Target.LookAtTarget = target;
    }
}
