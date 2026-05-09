using System.Collections.Generic;
using UnityEngine;

public struct HighlightGridPayload
{
    public List<Vector3> InteractionArea;
    public List<Vector3> AffectedCells;
    public bool IsValidHighlight;
    public Vector3? Origin;
    public List<Vector3Int> OceanCurrentCells; // null = no update, empty = clear

    public HighlightGridPayload(List<Vector3> affectedCells, List<Vector3> interactionArea, bool isValidHighlight, Vector3 origin)
    {
        this.AffectedCells = affectedCells;
        this.InteractionArea = interactionArea;
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
        this.OceanCurrentCells = null;
    }

    public HighlightGridPayload(List<Vector3> affectedCells, List<Vector3> interactionArea, bool isValidHighlight)
    {
        this.AffectedCells = affectedCells;
        this.IsValidHighlight = isValidHighlight;
        this.InteractionArea = interactionArea;
        this.Origin = null;
        this.OceanCurrentCells = null;
    }

    public static HighlightGridPayload Empty => new()
    {
        IsValidHighlight = false,
        AffectedCells = new(),
        InteractionArea = new(),
        OceanCurrentCells = null
    };

    // Raise this to update ocean current tiles without touching the path preview.
    public static HighlightGridPayload OceanCurrentUpdate(List<Vector3Int> cells) => new()
    {
        AffectedCells = null,
        InteractionArea = null,
        OceanCurrentCells = cells ?? new List<Vector3Int>()
    };
}
