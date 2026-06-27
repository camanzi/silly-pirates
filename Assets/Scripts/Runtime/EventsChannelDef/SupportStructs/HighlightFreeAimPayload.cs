using System.Collections.Generic;
using UnityEngine;

public struct HighlightFreeAimPayload
{
    public List<ITargettable> Targets;
    public bool IsValidHighlight;
    public IInteractableElement Origin;
    public List<TrajectoryArc> Arcs;
    public ITargettable HoveredTarget;
    public float? HitChance;

    public HighlightFreeAimPayload(IInteractableElement origin, bool isValidHighlight, List<ITargettable> targets = null, List<TrajectoryArc> arcs = null)
    {
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
        this.Targets = targets;
        this.Arcs = arcs;
        this.HoveredTarget = null;
        this.HitChance = null;
    }

    public static HighlightFreeAimPayload Empty => new ();
}
