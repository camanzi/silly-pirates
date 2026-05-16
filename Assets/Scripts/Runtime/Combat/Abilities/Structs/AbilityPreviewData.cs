using System.Collections.Generic;
using UnityEngine;

public struct AbilityPreviewData
{
    public List<Vector3> InteractionArea;
    public List<Vector3> AffectedCells;
    public List<ITargettable> FreeAimTargets;
    public List<TrajectoryArc> Arcs;

    public AbilityPreviewData(List<Vector3> affectedCells, List<Vector3> interactionArea, List<ITargettable> freeAimTargets = null, List<TrajectoryArc> arcs = null)
    {
        this.AffectedCells = affectedCells;
        this.FreeAimTargets = freeAimTargets;
        this.InteractionArea = interactionArea;
        this.Arcs = arcs;
    }

    public static AbilityPreviewData Empty => new AbilityPreviewData {
        AffectedCells = new (),
        InteractionArea = new(),
        FreeAimTargets = null,
        Arcs = null
    };
}
