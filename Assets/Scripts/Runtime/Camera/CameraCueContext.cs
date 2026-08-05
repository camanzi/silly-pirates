using System;
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
    /// <summary>Supplies the i-th pooled ground anchor, creating it on demand.</summary>
    public Func<int, Transform> GroundAnchorProvider;
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
        Transform anchor = GroundAnchorProvider(0);
        anchor.position = point;
        TargetGroup.AddMember(anchor, 1f, Profile.MemberRadius);
    }

    /// <summary>
    /// Adds one group member for EVERY affected point in the cue, so the framing covers the AoE's real
    /// extent instead of just its centroid. Falls back to the single centroid/TargetPoint anchor when the
    /// cue carries no AffectedCells. Returns how many members were added.
    /// </summary>
    public int AddGroundAnchors()
    {
        if (Cue.AffectedCells == null || Cue.AffectedCells.Count == 0)
            return TryAddGroundAnchor() ? 1 : 0;

        for (int i = 0; i < Cue.AffectedCells.Count; i++)
        {
            Transform anchor = GroundAnchorProvider(i);
            anchor.position = Cue.AffectedCells[i];
            TargetGroup.AddMember(anchor, 1f, Profile.MemberRadius);
        }
        return Cue.AffectedCells.Count;
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
