using System.Collections.Generic;
using UnityEngine;

public struct HighlightFreeAimPayload
{
    public Vector3? MousePosition;
    public List<ITargettable> Targets;
    public bool IsValidHighlight;
    public IInteractableElement Origin;
    public TrajectoryConfigsSO TrajectoryConfigData;
    public HighlightFreeAimPayload(IInteractableElement origin, bool isValidHighlight, List<ITargettable> targets = null, Vector3? mousePosition = null, TrajectoryConfigsSO trajectoryConfigData = null)
    {
        this.MousePosition = mousePosition;
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
        this.Targets = targets;
        this.TrajectoryConfigData = trajectoryConfigData;
    }

    public static HighlightFreeAimPayload Empty => new ();
}
