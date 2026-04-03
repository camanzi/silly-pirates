using System.Collections.Generic;
using UnityEngine;

public struct HighlightGridPayload
{
    public List<Vector3> AffectedCells;
    public bool IsValidHighlight;
    public Vector3? Origin;
    public HighlightGridPayload(List<Vector3> affectedCells, bool isValidHighlight, Vector3 origin)
    {
        this.AffectedCells = affectedCells;
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
    }

    public HighlightGridPayload(List<Vector3> affectedCells, bool isValidHighlight)
    {
        this.AffectedCells = affectedCells;
        this.IsValidHighlight = isValidHighlight;
        this.Origin = null;
    }

    public static HighlightGridPayload Empty => new ()
    {
        IsValidHighlight = false,
        AffectedCells = new() 
    };
}
