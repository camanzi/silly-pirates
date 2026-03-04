using System.Collections.Generic;
using UnityEngine;

public struct HighlightCellsPayload
{
    public HighlightCellsPayload(ICollection<Vector3Int> cells, bool isValidHighlight)
    {
        this.cells = cells;
        this.isValidHighlight = isValidHighlight;
    }

    public ICollection<Vector3Int> cells;
    public bool isValidHighlight;
}
