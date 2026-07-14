using System.Collections.Generic;
using UnityEngine;

public struct AbilityExecutionCue
{
    public AbilityBase Ability;
    public IInteractableElement Caster;
    public IReadOnlyList<ITargettable> Targets;
    public IReadOnlyList<Vector3> AffectedCells;
    public Vector3? TargetPoint;

    public AbilityExecutionCue(AbilityBase ability, IInteractableElement caster, IReadOnlyList<ITargettable> targets, IReadOnlyList<Vector3> affectedCells, Vector3? targetPoint)
    {
        Ability = ability;
        Caster = caster;
        Targets = targets;
        AffectedCells = affectedCells;
        TargetPoint = targetPoint;
    }
}
