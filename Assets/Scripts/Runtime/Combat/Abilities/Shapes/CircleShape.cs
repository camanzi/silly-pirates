using System.Collections.Generic;
using UnityEngine;

public class CircleShape : IAreaShape
{
    public List<Vector3> GetCells(Vector3Int center, int range, Vector3Int casterPos)
    {
        var result = new List<Vector3>();
        var visited = new HashSet<Vector3Int> { center };
        var frontier = new List<Vector3Int> { center };
        result.Add((Vector3)center);

        for (int dist = 1; dist <= range; dist++)
        {
            var next = new List<Vector3Int>();
            foreach (var cell in frontier)
                foreach (var nb in PathFindingUtils.GetNeighbors(cell))
                    if (visited.Add(nb)) { result.Add((Vector3)nb); next.Add(nb); }
            frontier = next;
        }

        return result;
    }
}
