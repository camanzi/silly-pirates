using System;
using System.Collections.Generic;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    // The camera rig lives in other prefabs and so cannot be referenced from here: it arrives through
    // anchor SOs, resolved with the pull-then-subscribe contract documented on RuntimeAnchorSO.
    [Header("Camera Rig (anchors)")]
    [SerializeField] private CinemachineCameraAnchorSO _actionCameraAnchor;
    [SerializeField] private CinemachineTargetGroupAnchorSO _targetGroupAnchor;
    [SerializeField] private CinemachineBrainAnchorSO _brainAnchor;

    [Header("Dependencies")]
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;
    [SerializeField] private CameraCueDefaultProfilesSO _cueDefaults;

    [Tooltip("Anchor moved onto ground-targeted positions so the target group can frame abilities with no ITargettable")]
    [SerializeField] private Transform _groundAnchor;

    [Tooltip("Safety cap on how long a cue waits for the Brain blend before signaling ready")]
    [Min(0.5f)]
    [SerializeField] private float _maxBlendWait = 2f;

    private CinemachineCamera _actionCamera;
    private CinemachineTargetGroup _targetGroup;
    private CinemachineBrain _brain;

    private CinemachineGroupFraming _groupFraming;
    private CameraCueProfileSO _activeProfile;
    private bool _isCueActive;

    // Orbit drift. The action camera has no Aim stage, so its Transform rotation IS the vcam state
    // orientation: yawing it makes CinemachinePositionComposer re-anchor the camera around the tracked
    // point, i.e. a real orbit around the framed pivot. _baseRotation is captured once so repeated cues
    // can never accumulate drift.
    private Tween _orbitTween;
    private Quaternion _baseCameraRotation;

    // The angle the current cue is shot from: the authored _baseCameraRotation, unless the profile aims the
    // shot along the cue's ShotHeading. Recomputed at every cue, so a heading shot never leaks into the next.
    private Quaternion _cueBaseRotation;

    // Pool of ground anchors, grown on demand so a cue can frame several areas at once
    // (one member per affected point) rather than a single centroid.
    private readonly List<Transform> _groundAnchors = new();
    private Func<int, Transform> _groundAnchorProvider;

    private void Awake()
    {
        if (_groundAnchor == null)
        {
            _groundAnchor = new GameObject("CueGroundAnchor").transform;
            _groundAnchor.SetParent(transform);
        }
        _groundAnchors.Add(_groundAnchor);
        _groundAnchorProvider = GetGroundAnchor;
    }

    private Transform GetGroundAnchor(int index)
    {
        while (_groundAnchors.Count <= index)
        {
            var anchor = new GameObject($"CueGroundAnchor_{_groundAnchors.Count}").transform;
            anchor.SetParent(transform);
            _groundAnchors.Add(anchor);
        }
        return _groundAnchors[index];
    }

    private void OnEnable()
    {
        if (_directorState != null) _directorState.OnFocusEnded += OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised += HandleCue;

        // Pull-then-subscribe: the pull picks up the rig already in the scene, the subscribe picks up the
        // one arriving later (director in the persistent scene, rig in the additive combat one).
        if (_actionCameraAnchor != null)
        {
            HandleActionCameraChanged(_actionCameraAnchor.Value);
            _actionCameraAnchor.OnValueChanged += HandleActionCameraChanged;
        }
        if (_targetGroupAnchor != null)
        {
            _targetGroup = _targetGroupAnchor.Value;
            _targetGroupAnchor.OnValueChanged += HandleTargetGroupChanged;
        }
        if (_brainAnchor != null)
        {
            _brain = _brainAnchor.Value;
            _brainAnchor.OnValueChanged += HandleBrainChanged;
        }
    }

    private void OnDisable()
    {
        if (_directorState != null) _directorState.OnFocusEnded -= OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised -= HandleCue;

        if (_actionCameraAnchor != null) _actionCameraAnchor.OnValueChanged -= HandleActionCameraChanged;
        if (_targetGroupAnchor != null) _targetGroupAnchor.OnValueChanged -= HandleTargetGroupChanged;
        if (_brainAnchor != null) _brainAnchor.OnValueChanged -= HandleBrainChanged;

        StopOrbit();
    }

    private void HandleActionCameraChanged(CinemachineCamera camera)
    {
        // The orbit tween drives the previous camera's transform: it has to be stopped before that
        // reference is lost, or it keeps rotating an object we no longer govern.
        _orbitTween.Stop();

        _actionCamera = camera;
        if (camera == null)
        {
            _groupFraming = null;
            return;
        }

        // Captured here — not in Awake, and not "on the first cue behind a one-shot flag". The session's
        // second combat brings a NEW action camera instance: a one-shot capture would use the rotation of
        // the previous, now destroyed one as its base. At this point the transform is still the authored
        // one, because no cue can start before the rig has registered itself.
        _groupFraming = camera.GetComponent<CinemachineGroupFraming>();
        _baseCameraRotation = camera.transform.rotation;
        _cueBaseRotation = _baseCameraRotation;
    }

    private void HandleTargetGroupChanged(CinemachineTargetGroup targetGroup) => _targetGroup = targetGroup;

    private void HandleBrainChanged(CinemachineBrain brain) => _brain = brain;

    public void HandleCue(AbilityExecutionCue cue)
    {
        CameraCueType cueType = cue.CueTypeOverride ?? (cue.Ability != null ? cue.Ability.CameraCue : CameraCueType.None);
        CameraCueProfileSO profile = cue.ProfileOverride != null
            ? cue.ProfileOverride
            : cue.Ability != null && cue.Ability.CameraCueProfile != null
                ? cue.Ability.CameraCueProfile
                : _cueDefaults != null ? _cueDefaults.GetProfile(cueType) : null;

        ICameraCueHandler handler = CameraCueHandlerFactory.GetHandler(cueType);

        if (handler == null || profile == null || _actionCamera == null || _targetGroup == null)
        {
            if (_directorState != null) _directorState.SignalFocusReady();
            return;
        }

        var context = new CameraCueContext {
            TargetGroup = _targetGroup,
            GroupFraming = _groupFraming,
            Brain = _brain,
            GroundAnchorProvider = _groundAnchorProvider,
            Cue = cue,
            Profile = profile,
            MaxBlendWait = _maxBlendWait
        };

        RunCueAsync(handler, context);
    }

    private async void RunCueAsync(ICameraCueHandler handler, CameraCueContext context)
    {
        // Back-to-back cues (intro beats) re-enter here without an intervening release, so the previous
        // sweep must die and the yaw be re-seeded BEFORE the reframe — the jump then hides inside the
        // repositioning caused by the group being cleared and repopulated.
        _cueBaseRotation = ComputeCueBaseRotation(context.Cue, context.Profile);
        SeedOrbitStart(context.Profile);

        _activeProfile = context.Profile;
        _isCueActive = true;

        if (_groupFraming != null) _groupFraming.FramingSize = context.Profile.FramingSize;
        _actionCamera.enabled = true;

        await handler.RunAsync(context);

        // Fire-and-forget: the shot keeps drifting while the caller proceeds with the ability/spawn.
        StartOrbit(context.Profile);

        if (_directorState != null) _directorState.SignalFocusReady();
    }

    /// <summary>
    /// Kills any running sweep and parks the vcam at the orbit's starting yaw (-half the sweep), so the
    /// shot blends in already rotated and the drift crosses the cue's base angle instead of leaving it.
    /// Cues without an orbit are parked on the cue's base angle (the authored one unless the profile aims
    /// along the cue's heading).
    /// </summary>
    private void SeedOrbitStart(CameraCueProfileSO profile)
    {
        _orbitTween.Stop();
        if (_actionCamera == null) return;

        bool hasOrbit = profile != null && profile.HasOrbit;
        _actionCamera.transform.rotation = OrbitRotation(hasOrbit ? -HalfSweep(profile) : 0f);
    }

    private void StartOrbit(CameraCueProfileSO profile)
    {
        if (profile == null || !profile.HasOrbit || _actionCamera == null) return;

        float half = HalfSweep(profile);
        float duration = profile.OrbitMaxDegrees / Mathf.Abs(profile.OrbitSpeed);

        _orbitTween = Tween.Rotation(_actionCamera.transform,
            startValue: OrbitRotation(-half),
            endValue: OrbitRotation(half),
            duration, profile.OrbitEase);
    }

    private void StopOrbit()
    {
        _orbitTween.Stop();
        if (_actionCamera != null) _actionCamera.transform.rotation = _baseCameraRotation;
    }

    // Signed: OrbitSpeed's sign is the sweep direction, OrbitMaxDegrees its total travel.
    private static float HalfSweep(CameraCueProfileSO profile)
        => Mathf.Sign(profile.OrbitSpeed) * profile.OrbitMaxDegrees * 0.5f;

    private Quaternion OrbitRotation(float degrees)
        => Quaternion.AngleAxis(degrees, Vector3.up) * _cueBaseRotation;

    /// <summary>
    /// The authored angle, unless the profile asks to shoot relative to the cue's heading and the cue has
    /// one: then the view is turned <see cref="CameraCueProfileSO.YawOffset"/> degrees from the heading and
    /// tilted by <see cref="CameraCueProfileSO.Pitch"/>. With <see cref="CameraCueProfileSO.MirrorTowardGroup"/>
    /// the offset's sign is chosen so the view looks toward the side the targets are on.
    /// </summary>
    private Quaternion ComputeCueBaseRotation(AbilityExecutionCue cue, CameraCueProfileSO profile)
    {
        if (profile == null || !profile.UseCueHeading || !cue.ShotHeading.HasValue) return _baseCameraRotation;

        Vector3 heading = cue.ShotHeading.Value;
        heading.y = 0f;
        if (heading.sqrMagnitude < 0.0001f) return _baseCameraRotation;
        heading.Normalize();

        float offset = profile.YawOffset;
        if (profile.MirrorTowardGroup && TryGetTargetSide(cue, heading, out float side))
            offset = Mathf.Abs(offset) * side;

        float headingYaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(profile.Pitch, headingYaw + offset, 0f);
    }

    /// <summary>
    /// Which side of the heading the targets sit on, seen from the cue's origin (caster and affected points,
    /// or TargetPoint): +1 = clockwise from above, i.e. the side a positive yaw turns toward. False when
    /// there are no targets or no origin to measure from, or when the targets sit right on the heading line.
    /// </summary>
    private static bool TryGetTargetSide(AbilityExecutionCue cue, Vector3 heading, out float side)
    {
        side = 1f;

        if (!TryGetTargetsCentroid(cue, out Vector3 targets)) return false;
        if (!TryGetOriginCentroid(cue, out Vector3 origin)) return false;

        float cross = Vector3.Cross(heading, targets - origin).y;
        if (Mathf.Abs(cross) < 0.01f) return false;

        side = Mathf.Sign(cross);
        return true;
    }

    private static bool TryGetTargetsCentroid(AbilityExecutionCue cue, out Vector3 centroid)
    {
        centroid = Vector3.zero;
        int count = 0;
        if (cue.Targets != null)
        {
            for (int i = 0; i < cue.Targets.Count; i++)
            {
                ITargettable target = cue.Targets[i];
                if (target == null || target.Transform == null) continue;
                centroid += target.Transform.position;
                count++;
            }
        }

        if (count == 0) return false;
        centroid /= count;
        return true;
    }

    private static bool TryGetOriginCentroid(AbilityExecutionCue cue, out Vector3 centroid)
    {
        centroid = Vector3.zero;
        int count = 0;

        if (cue.Caster != null && cue.Caster.Transform != null)
        {
            centroid += cue.Caster.Transform.position;
            count++;
        }
        if (cue.AffectedCells != null)
        {
            for (int i = 0; i < cue.AffectedCells.Count; i++)
            {
                centroid += cue.AffectedCells[i];
                count++;
            }
        }
        if (count == 0 && cue.TargetPoint.HasValue)
        {
            centroid = cue.TargetPoint.Value;
            count = 1;
        }

        if (count == 0) return false;
        centroid /= count;
        return true;
    }

    private void OnFocusEnded()
    {
        if (!_isCueActive) return;
        _isCueActive = false;
        ReleaseAsync();
    }

    private async void ReleaseAsync()
    {
        float hold = _activeProfile != null ? _activeProfile.PostShotHold : 0f;
        if (hold > 0f)
            await Awaitable.WaitForSecondsAsync(hold);

        // A new cue may have started during the hold — don't steal its camera
        if (_isCueActive) return;

        // Only the drift stops here: the rotation is left where the shot ended. Snapping it back to the
        // authored angle in the same frame the vcam is disabled would make the blend-out start from a
        // different angle than the one on screen (a visible jump with a heading shot ~145 degrees away).
        // The next cue re-seeds the rotation anyway (SeedOrbitStart), so nothing accumulates.
        _orbitTween.Stop();
        // The rig may have gone away during the hold (a scene unload): StopOrbit tolerates that already,
        // this does not.
        if (_actionCamera != null) _actionCamera.enabled = false;
        _activeProfile = null;
    }
}
