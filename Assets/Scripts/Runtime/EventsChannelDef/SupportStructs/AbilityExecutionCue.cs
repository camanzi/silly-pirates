using System.Collections.Generic;
using UnityEngine;

public struct AbilityExecutionCue
{
    public AbilityBase Ability;
    public IInteractableElement Caster;
    public IReadOnlyList<ITargettable> Targets;
    public IReadOnlyList<Vector3> AffectedCells;
    public Vector3? TargetPoint;

    /// <summary>Overrides the cue type/profile normally read from Ability, for callers that raise ad-hoc cues mid-command.</summary>
    public CameraCueType? CueTypeOverride;
    public CameraCueProfileSO ProfileOverride;

    /// <summary>
    /// Optional world-space reference direction (only its horizontal part is used) for profiles that aim
    /// the shot relative to the subject, e.g. the ship's bow. Null = the action camera's authored angle.
    /// </summary>
    public Vector3? ShotHeading;

    public AbilityExecutionCue(AbilityBase ability, IInteractableElement caster, IReadOnlyList<ITargettable> targets, IReadOnlyList<Vector3> affectedCells, Vector3? targetPoint)
    {
        Ability = ability;
        Caster = caster;
        Targets = targets;
        AffectedCells = affectedCells;
        TargetPoint = targetPoint;
        CueTypeOverride = null;
        ProfileOverride = null;
        ShotHeading = null;
    }
}
