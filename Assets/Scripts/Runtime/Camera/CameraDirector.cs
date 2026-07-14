using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CinemachineCamera _actionCamera;
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;
    [SerializeField] private CameraCueProfileSO _defaultProfile;

    [Tooltip("Anchor moved onto ground-targeted positions so the target group can frame abilities with no ITargettable")]
    [SerializeField] private Transform _groundAnchor;

    [Tooltip("Safety cap on how long a cue waits for the Brain blend before signaling ready")]
    [Min(0.5f)]
    [SerializeField] private float _maxBlendWait = 2f;

    private CinemachineGroupFraming _groupFraming;
    private CameraCueProfileSO _activeProfile;
    private bool _isCueActive;

    private void Awake()
    {
        if (_actionCamera != null) _groupFraming = _actionCamera.GetComponent<CinemachineGroupFraming>();
        if (_groundAnchor == null)
        {
            _groundAnchor = new GameObject("CueGroundAnchor").transform;
            _groundAnchor.SetParent(transform);
        }
    }

    private void OnEnable()
    {
        if (_directorState != null) _directorState.OnFocusEnded += OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised += HandleCue;
    }

    private void OnDisable()
    {
        if (_directorState != null) _directorState.OnFocusEnded -= OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised -= HandleCue;
    }

    public void HandleCue(AbilityExecutionCue cue)
    {
        CameraCueType cueType = cue.Ability != null ? cue.Ability.CameraCue : CameraCueType.None;
        CameraCueProfileSO profile = cue.Ability != null && cue.Ability.CameraCueProfile != null
            ? cue.Ability.CameraCueProfile
            : _defaultProfile;

        if (cueType == CameraCueType.None || profile == null || _actionCamera == null || _targetGroup == null || cue.Caster == null)
        {
            _directorState?.SignalFocusReady();
            return;
        }

        PopulateTargetGroup(cueType, cue, profile);
        RunCueAsync(profile);
    }

    private void PopulateTargetGroup(CameraCueType cueType, AbilityExecutionCue cue, CameraCueProfileSO profile)
    {
        _targetGroup.Targets.Clear();
        _targetGroup.AddMember(cue.Caster.Transform, 1f, profile.MemberRadius);

        if (cueType == CameraCueType.FocusCaster) return;

        // FramePair and every not-yet-implemented cue type frame caster + resolved targets
        bool hasTargetMember = false;
        if (cue.Targets != null)
        {
            for (int i = 0; i < cue.Targets.Count; i++)
            {
                ITargettable target = cue.Targets[i];
                if (target == null || target.Transform == null) continue;
                _targetGroup.AddMember(target.Transform, 1f, profile.MemberRadius);
                hasTargetMember = true;
            }
        }

        if (!hasTargetMember && TryGetGroundPoint(cue, out Vector3 groundPoint))
        {
            _groundAnchor.position = groundPoint;
            _targetGroup.AddMember(_groundAnchor, 1f, profile.MemberRadius);
        }
    }

    private static bool TryGetGroundPoint(AbilityExecutionCue cue, out Vector3 point)
    {
        if (cue.AffectedCells != null && cue.AffectedCells.Count > 0)
        {
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < cue.AffectedCells.Count; i++) centroid += cue.AffectedCells[i];
            point = centroid / cue.AffectedCells.Count;
            return true;
        }

        if (cue.TargetPoint.HasValue)
        {
            point = cue.TargetPoint.Value;
            return true;
        }

        point = default;
        return false;
    }

    private async void RunCueAsync(CameraCueProfileSO profile)
    {
        _activeProfile = profile;
        _isCueActive = true;

        if (_groupFraming != null) _groupFraming.FramingSize = profile.FramingSize;
        _actionCamera.enabled = true;

        // Give the Brain a frame to start blending, then wait for the blend to settle
        float deadline = Time.time + _maxBlendWait;
        await Awaitable.NextFrameAsync();
        while (_brain != null && _brain.IsBlending && Time.time < deadline)
            await Awaitable.NextFrameAsync();

        if (profile.PreShotHold > 0f)
            await Awaitable.WaitForSecondsAsync(profile.PreShotHold);

        _directorState?.SignalFocusReady();
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

        _actionCamera.enabled = false;
        _activeProfile = null;
    }
}
