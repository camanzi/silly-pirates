using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Why a footprint cell can or cannot be used.</summary>
public enum FootprintCellStatus
{
    /// <summary>Walkable and free.</summary>
    Valid = 0,
    /// <summary>No tile under the cell, or a tile that is not walkable. Checked first: a cell that is not
    /// walkable is never reported as Overlap.</summary>
    NotWalkable = 1,
    /// <summary>Walkable, but already taken by another element.</summary>
    Overlap = 2,
}

/// <summary>One footprint cell and its placement status.</summary>
public readonly struct FootprintCell
{
    public readonly Vector3Int Cell;
    public readonly FootprintCellStatus Status;

    public FootprintCell(Vector3Int cell, FootprintCellStatus status)
    {
        Cell = cell;
        Status = status;
    }

    public bool IsValid => Status == FootprintCellStatus.Valid;
}

/// <summary>
/// Pure placement validation of a set of footprint cells. The rules come in through delegates (same pattern
/// as <see cref="PathFindingUtils"/>), so the same code serves the editor preview (floor tilemap plus the
/// other equipment's cells) and a future runtime layout tool (<see cref="GridStateDataSO"/> occupancy).
/// No Unity objects involved: EditMode-testable.
/// </summary>
public static class FootprintPlacement
{
    /// <summary>
    /// Clears <paramref name="results"/> and fills it with every one of <paramref name="cells"/>, in the same
    /// order (index 0 is the anchor when the cells come from <see cref="GridElement"/>), each with its status.
    /// NotWalkable takes precedence over Overlap.
    /// </summary>
    /// <param name="isWalkable">True when the cell can hold the element (see <see cref="GridUtils.IsWalkable"/>).
    /// Null means every cell is walkable.</param>
    /// <param name="isTakenByOther">True when another element already covers the cell (the caller must
    /// exclude the element being placed). Null means nothing is taken.</param>
    /// <returns>True when every cell is <see cref="FootprintCellStatus.Valid"/>.</returns>
    public static bool Evaluate(IReadOnlyList<Vector3Int> cells,
                                Func<Vector3Int, bool> isWalkable, Func<Vector3Int, bool> isTakenByOther,
                                List<FootprintCell> results)
    {
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        if (results == null) throw new ArgumentNullException(nameof(results));

        results.Clear();

        bool allValid = true;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            FootprintCellStatus status;
            if (isWalkable != null && !isWalkable(cell)) status = FootprintCellStatus.NotWalkable;
            else if (isTakenByOther != null && isTakenByOther(cell)) status = FootprintCellStatus.Overlap;
            else status = FootprintCellStatus.Valid;

            if (status != FootprintCellStatus.Valid) allValid = false;
            results.Add(new FootprintCell(cell, status));
        }
        return allValid;
    }
}
