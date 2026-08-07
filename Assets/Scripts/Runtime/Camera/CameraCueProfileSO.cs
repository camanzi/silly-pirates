using PrimeTween;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraCueProfile", menuName = "Camera/Camera Cue Profile")]
public class CameraCueProfileSO : ScriptableObject
{
    [Header("Framing")]
    [Tooltip("Screen fraction the caster+targets group should occupy (CinemachineGroupFraming.FramingSize)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float _framingSize = 0.8f;

    [Tooltip("Radius attributed to each group member, adds breathing room around characters")]
    [Min(0f)]
    [SerializeField] private float _memberRadius = 1.5f;

    [Header("Timing")]
    [Tooltip("Extra hold on the framed shot AFTER the blend completes, before the ability effect starts")]
    [Min(0f)]
    [SerializeField] private float _preShotHold = 0.25f;

    [Tooltip("Hold AFTER the command finished, before releasing the camera back to the player")]
    [Min(0f)]
    [SerializeField] private float _postShotHold = 0.4f;

    [Header("Orbit")]
    [Tooltip("Degrees per second the action camera drifts around the framed pivot while the shot is held. 0 = no orbit. The sign flips the direction")]
    [SerializeField] private float _orbitSpeed = 0f;

    [Tooltip("Total degrees travelled before the drift stops. The sweep is centred on the authored angle: the shot enters at -half and ends at +half")]
    [Min(0.5f)]
    [SerializeField] private float _orbitMaxDegrees = 10f;

    [Tooltip("Orbit curve. Linear = constant speed, OutSine = soft arrival at the clamp")]
    [SerializeField] private Ease _orbitEase = Ease.Linear;

    public float FramingSize => _framingSize;
    public float MemberRadius => _memberRadius;
    public float PreShotHold => _preShotHold;
    public float PostShotHold => _postShotHold;
    public float OrbitSpeed => _orbitSpeed;
    public float OrbitMaxDegrees => _orbitMaxDegrees;
    public Ease OrbitEase => _orbitEase;
    public bool HasOrbit => Mathf.Abs(_orbitSpeed) > 0.01f;
}
