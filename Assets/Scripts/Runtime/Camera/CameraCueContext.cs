using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Per-cue scratch state and helper operations handed to an <see cref="ICameraCueHandler"/>.
/// Owns target-group population and blend/hold timing so each handler can compose its shot
/// without reaching back into <see cref="CameraDirector"/>.
/// </summary>
public class CameraCueContext
{
    public CinemachineTargetGroup TargetGroup;
    public CinemachineGroupFraming GroupFraming;
    public CinemachineBrain Brain;
    public Transform GroundAnchor;
    public AbilityExecutionCue Cue;
    public CameraCueProfileSO Profile;
    public float MaxBlendWait;

    public void ClearGroup()
    {
        TargetGroup.Targets.Clear();
    }

    public void AddCaster()
    {
        TargetGroup.AddMember(Cue.Caster.Transform, 1f, Profile.MemberRadius);
    }

    public bool AddTargets()
    {
        bool hasTargetMember = false;
        if (Cue.Targets != null)
        {
            for (int i = 0; i < Cue.Targets.Count; i++)
            {
                ITargettable target = Cue.Targets[i];
                if (target == null || target.Transform == null) continue;
                TargetGroup.AddMember(target.Transform, 1f, Profile.MemberRadius);
                hasTargetMember = true;
            }
        }

        return hasTargetMember;
    }

    public bool TryAddGroundAnchor()
    {
        if (!TryGetGroundPoint(out Vector3 point)) return false;
        AddGroundAnchor(point);
        return true;
    }

    public void AddGroundAnchor(Vector3 point)
    {
        GroundAnchor.position = point;
        TargetGroup.AddMember(GroundAnchor, 1f, Profile.MemberRadius);
    }

    public async Awaitable WaitForBlendSettle()
    {
        float deadline = Time.time + MaxBlendWait;
        await Awaitable.NextFrameAsync();
        while (Brain != null && Brain.IsBlending && Time.time < deadline)
            await Awaitable.NextFrameAsync();
    }

    public async Awaitable Hold(float seconds)
    {
        if (seconds > 0f)
            await Awaitable.WaitForSecondsAsync(seconds);
    }

    private bool TryGetGroundPoint(out Vector3 point)
    {
        if (Cue.AffectedCells != null && Cue.AffectedCells.Count > 0)
        {
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < Cue.AffectedCells.Count; i++) centroid += Cue.AffectedCells[i];
            point = centroid / Cue.AffectedCells.Count;
            return true;
        }

        if (Cue.TargetPoint.HasValue)
        {
            point = Cue.TargetPoint.Value;
            return true;
        }

        point = default;
        return false;
    }
}
