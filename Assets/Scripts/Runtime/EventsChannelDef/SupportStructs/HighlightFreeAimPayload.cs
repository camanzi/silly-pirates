using System.Collections.Generic;
using UnityEngine;

public struct HighlightFreeAimPayload
{
    public List<ITargettable> Targets;
    public bool IsValidHighlight;
    public IInteractableElement Origin;
    public List<TrajectoryArc> Arcs;

    public HighlightFreeAimPayload(IInteractableElement origin, bool isValidHighlight, List<ITargettable> targets = null, List<TrajectoryArc> arcs = null)
    {
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
        this.Targets = targets;
        this.Arcs = arcs;
    }

    public static HighlightFreeAimPayload Empty => new ();
}
