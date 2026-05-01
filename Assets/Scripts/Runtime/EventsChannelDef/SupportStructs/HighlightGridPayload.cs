using System.Collections.Generic;
using UnityEngine;

public struct HighlightGridPayload
{
    public List<Vector3> InteractionArea;
    public List<Vector3> AffectedCells;
    public bool IsValidHighlight;
    public Vector3? Origin;
    public HighlightGridPayload(List<Vector3> affectedCells, List<Vector3> interactionArea, bool isValidHighlight, Vector3 origin)
    {
        this.AffectedCells = affectedCells;
        this.InteractionArea = interactionArea;
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
    }

    public HighlightGridPayload(List<Vector3> affectedCells, List<Vector3> interactionArea, bool isValidHighlight)
    {
        this.AffectedCells = affectedCells;
        this.IsValidHighlight = isValidHighlight;
        this.InteractionArea = interactionArea;
        this.Origin = null;
    }

    public static HighlightGridPayload Empty => new ()
    {
        IsValidHighlight = false,
        AffectedCells = new(),
        InteractionArea = new()
    };
}
