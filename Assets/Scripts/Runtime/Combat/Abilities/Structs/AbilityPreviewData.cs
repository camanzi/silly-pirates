using System.Collections.Generic;
using UnityEngine;

public struct AbilityPreviewData
{
    public List<Vector3> InteractionArea;
    public List<Vector3> AffectedCells;
    public List<ITargettable> FreeAimTargets;
    public List<TrajectoryArc> CustomArcs; // null = none

    public AbilityPreviewData(List<Vector3> affectedCells, List<Vector3> interactionArea, List<ITargettable> freeAimTargets = null, List<TrajectoryArc> customArcs = null)
    {
        this.AffectedCells = affectedCells;
        this.FreeAimTargets = freeAimTargets;
        this.InteractionArea = interactionArea;
        this.CustomArcs = customArcs;
    }

    public static AbilityPreviewData Empty => new AbilityPreviewData {
        AffectedCells = new (),
        InteractionArea = new(),
        FreeAimTargets = null,
        CustomArcs = null
    };
}
