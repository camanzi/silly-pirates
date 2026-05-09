using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class OceanCurrentDistributor
{
    private static readonly Vector3Int[] OddRowNeighbors = {
        new Vector3Int(0, 1, 0),  new Vector3Int(1, 1, 0),  new Vector3Int(1, 0, 0),
        new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0)
    };
    private static readonly Vector3Int[] EvenRowNeighbors = {
        new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0),  new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0)
    };

    private enum Side { Left, Right, Top, Bottom }

    public static List<Vector3Int> FindExteriorBorderCells(Tilemap floorMap)
    {
        var candidates = new HashSet<Vector3Int>();

        foreach (Vector3Int pos in floorMap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = floorMap.GetTile<TerrainTile>(pos);
            if (tile == null || !tile.isWalkable) continue;

            Vector3Int[] dirs = (pos.y & 1) != 0 ? OddRowNeighbors : EvenRowNeighbors;
            foreach (Vector3Int dir in dirs)
            {
                Vector3Int neighbor = pos + dir;
                TerrainTile neighborTile = floorMap.GetTile<TerrainTile>(neighbor);
                if (neighborTile == null)
                    candidates.Add(neighbor);
            }
        }

        return new List<Vector3Int>(candidates);
    }

    public static List<Vector3Int> Distribute(List<Vector3Int> candidates, int count, Vector3 gridCenter, int seed)
    {
        if (count <= 0 || candidates.Count == 0) return new List<Vector3Int>();

        var rng = new System.Random(seed);
        var shuffled = candidates.OrderBy(_ => rng.Next()).ToList();

        Vector2 center = new Vector2(gridCenter.x, gridCenter.y);
        var pools = new Dictionary<Side, List<Vector3Int>>
        {
            { Side.Left,   new List<Vector3Int>() },
            { Side.Right,  new List<Vector3Int>() },
            { Side.Top,    new List<Vector3Int>() },
            { Side.Bottom, new List<Vector3Int>() },
        };

        foreach (var c in shuffled)
            pools[Classify(c, center)].Add(c);

        int baseCount = count / 4;
        int rem = count % 4;
        int[] targets = {
            baseCount + (rem > 0 ? 1 : 0), // Left
            baseCount + (rem > 1 ? 1 : 0), // Right
            baseCount + (rem > 2 ? 1 : 0), // Top
            baseCount                        // Bottom
        };

        Side[] sides = { Side.Left, Side.Right, Side.Top, Side.Bottom };
        var selected = new List<Vector3Int>();

        for (int i = 0; i < sides.Length; i++)
        {
            int filled = 0;
            foreach (var candidate in pools[sides[i]])
            {
                if (filled >= targets[i]) break;
                if (IsFarEnough(candidate, selected))
                {
                    selected.Add(candidate);
                    filled++;
                }
            }
        }

        return selected;
    }

    private static bool IsFarEnough(Vector3Int candidate, List<Vector3Int> selected)
    {
        foreach (var s in selected)
        {
            if (PathFindingUtils.GetNormalizedDistance(candidate, s) < 2) return false;
        }
        return true;
    }

    private static Side Classify(Vector3Int pos, Vector2 center)
    {
        float dx = pos.x - center.x;
        float dy = pos.y - center.y;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            return dx >= 0 ? Side.Right : Side.Left;
        else
            return dy > 0 ? Side.Top : Side.Bottom;
    }
}
