using System.Collections.Generic;
using UnityEngine;

public class StepState
{
    public List<Vector3Int> AllCells = new();
    public List<(Vector3 Center, List<Vector3Int> Cells)> Zones = new();
    public List<ITargettable> SelectedTargets = new();
    public Dictionary<string, object> Extra = new();

    public IReadOnlyList<Vector3> AffectedWorldPoints =>
        Zones is { Count: > 0 } ? Zones.ConvertAll(z => z.Center) : null;
}
