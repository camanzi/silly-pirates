using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class PathFindingUtils
{
    private static readonly Vector3Int[] OddRowNeighbors = {
        new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0),
        new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0)
    };

    private static readonly Vector3Int[] EvenRowNeighbors = {
        new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0)
    };

    public static List<Vector3> FindPath<TState>(Vector3Int startPos, Vector3Int endPos, TState state, Func<Vector3Int, TState, bool> walkabilityCheck)
    {
        return FindPath(startPos, endPos, false, state, walkabilityCheck);
    }

    public static List<Vector3> FindPath<TState>(Vector3Int startPos, Vector3Int endPos, TState state, Func<Vector3Int, TState, bool> walkabilityCheck, Func<Vector3Int, int> cellCostGetter)
    {
        return FindPath(startPos, endPos, false, state, walkabilityCheck, cellCostGetter);
    }

    public static List<Vector3> FindPath<TState>(Vector3Int startPos, Vector3Int endPos, bool includeStartingPosition, TState state, Func<Vector3Int, TState, bool> walkabilityCheck, Func<Vector3Int, int> cellCostGetter)
    {
        List<Vector3Int> openSet = new List<Vector3Int> { startPos };
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3, AStarNode> allNodes = new Dictionary<Vector3, AStarNode>();

        allNodes[startPos] = new AStarNode(startPos, true, 0, GetDistance(startPos, endPos), null);

        while (openSet.Count > 0)
        {
            Vector3Int currentPos = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (allNodes[openSet[i]].fCost < allNodes[currentPos].fCost ||
                    (allNodes[openSet[i]].fCost == allNodes[currentPos].fCost && allNodes[openSet[i]].hCost < allNodes[currentPos].hCost))
                {
                    currentPos = openSet[i];
                }
            }

            openSet.Remove(currentPos);
            closedSet.Add(currentPos);

            if (currentPos == endPos)
                return RetracePath(allNodes, startPos, endPos, includeStartingPosition);

            foreach (Vector3Int neighborPos in GetNeighbors(currentPos))
            {
                if (closedSet.Contains(neighborPos) || !walkabilityCheck(neighborPos, state))
                    continue;

                int newMovementCostToNeighbor = allNodes[currentPos].gCost + cellCostGetter(neighborPos) * 10;

                if (!allNodes.ContainsKey(neighborPos) || newMovementCostToNeighbor < allNodes[neighborPos].gCost)
                {
                    AStarNode neighborNode = new AStarNode(
                        neighborPos, true,
                        newMovementCostToNeighbor,
                        GetDistance(neighborPos, endPos),
                        currentPos);

                    allNodes[neighborPos] = neighborNode;

                    if (!openSet.Contains(neighborPos))
                        openSet.Add(neighborPos);
                }
            }
        }

        return new List<Vector3>();
    }
    public static List<Vector3> FindPath<TState>(Vector3Int startPos, Vector3Int endPos, bool includeStartingPosition, TState state, Func<Vector3Int, TState, bool> walkabilityCheck)
    {
        List<Vector3Int> openSet = new List<Vector3Int> { startPos };
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3, AStarNode> allNodes = new Dictionary<Vector3, AStarNode>();

        allNodes[startPos] = new AStarNode(startPos, true, 0, GetDistance(startPos, endPos), null);

        while (openSet.Count > 0)
        {
            // Prendiamo il nodo con il fCost minore (o hCost se fCost è uguale)
            Vector3Int currentPos = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (allNodes[openSet[i]].fCost < allNodes[currentPos].fCost || 
                    (allNodes[openSet[i]].fCost == allNodes[currentPos].fCost && allNodes[openSet[i]].hCost < allNodes[currentPos].hCost))
                {
                    currentPos = openSet[i];
                }
            }

            openSet.Remove(currentPos);
            closedSet.Add(currentPos);

            if (currentPos == endPos)
            {
                return RetracePath(allNodes, startPos, endPos, includeStartingPosition);
            }

            foreach (Vector3Int neighborPos in GetNeighbors(currentPos))
            {
                if (closedSet.Contains(neighborPos) || !walkabilityCheck(neighborPos, state))
                    continue;

                int newMovementCostToNeighbor = allNodes[currentPos].gCost + 10; // 10 è il costo base per ogni tile

                if (!allNodes.ContainsKey(neighborPos) || newMovementCostToNeighbor < allNodes[neighborPos].gCost)
                {
                    AStarNode neighborNode = new AStarNode(
                        neighborPos,
                        true,
                        newMovementCostToNeighbor,
                        GetDistance(neighborPos, endPos),
                        currentPos
                    );

                    allNodes[neighborPos] = neighborNode;

                    if (!openSet.Contains(neighborPos))
                        openSet.Add(neighborPos);
                }
            }
        }

        return new List<Vector3>();
    }

    public static List<Vector3> FindReachableArea<TState>(Vector3Int startPos, int maxMovementCost, TState state, Func<Vector3Int, TState, bool> walkabilityCheck)
    {
        List<Vector3Int> openSet = new List<Vector3Int> { startPos };
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        costSoFar[startPos] = 0;

        List<Vector3> reachableArea = new List<Vector3>();

        while (openSet.Count > 0)
        {
            Vector3Int currentPos = openSet[0];
            int currentCost = costSoFar[currentPos];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (costSoFar[openSet[i]] < currentCost)
                {
                    currentPos = openSet[i];
                    currentCost = costSoFar[currentPos];
                }
            }

            openSet.Remove(currentPos);
            reachableArea.Add(currentPos);

            foreach (Vector3Int neighborPos in GetNeighbors(currentPos))
            {
                if (!walkabilityCheck(neighborPos, state))
                    continue;

                int newCost = costSoFar[currentPos] + 1; // Ogni movimento costa 1

                if (newCost <= maxMovementCost)
                {
                    if (!costSoFar.ContainsKey(neighborPos) || newCost < costSoFar[neighborPos])
                    {
                        costSoFar[neighborPos] = newCost;
                        if (!openSet.Contains(neighborPos))
                            openSet.Add(neighborPos);
                    }
                }
            }
        }

        return reachableArea;
    }

    public static List<Vector3> FindReachableArea<TState>(Vector3Int startPos, int maxMovementCost, TState state, Func<Vector3Int, TState, bool> walkabilityCheck, Func<Vector3Int, int> cellCostGetter)
    {
        List<Vector3Int> openSet = new List<Vector3Int> { startPos };
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        costSoFar[startPos] = 0;

        List<Vector3> reachableArea = new List<Vector3>();

        while (openSet.Count > 0)
        {
            Vector3Int currentPos = openSet[0];
            int currentCost = costSoFar[currentPos];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (costSoFar[openSet[i]] < currentCost)
                {
                    currentPos = openSet[i];
                    currentCost = costSoFar[currentPos];
                }
            }

            openSet.Remove(currentPos);
            reachableArea.Add(currentPos);

            foreach (Vector3Int neighborPos in GetNeighbors(currentPos))
            {
                if (!walkabilityCheck(neighborPos, state))
                    continue;

                int newCost = costSoFar[currentPos] + cellCostGetter(neighborPos);

                if (newCost <= maxMovementCost)
                {
                    if (!costSoFar.ContainsKey(neighborPos) || newCost < costSoFar[neighborPos])
                    {
                        costSoFar[neighborPos] = newCost;
                        if (!openSet.Contains(neighborPos))
                            openSet.Add(neighborPos);
                    }
                }
            }
        }

        return reachableArea;
    }

    public static List<Vector3Int> GetNeighbors(Vector3Int pos)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        Vector3Int[] directions = (pos.y & 1) != 0 ? OddRowNeighbors : EvenRowNeighbors;

        foreach (Vector3Int dir in directions)
        {
            neighbors.Add(pos + dir);
        }
        return neighbors;
    }

    public static int GetNormalizedDistance(Vector3Int a, Vector3Int b)
    {
        return GetDistance(a, b) / 10;
    }

    public static int GetDistance(Vector3Int a, Vector3Int b)
    {
        // Per gli esagoni la distanza Manhattan non funziona. Usiamo le coordinate assiali/cubiche.
        Vector3Int ac = OffsetToCube(a);
        Vector3Int bc = OffsetToCube(b);
        return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) / 2 * 10;
    }

    private static Vector3Int OffsetToCube(Vector3Int hex)
    {
        int q = hex.x - (hex.y - (hex.y & 1)) / 2;
        int r = hex.y;
        return new Vector3Int(q, r, -q - r);
    }

    private static List<Vector3> RetracePath(Dictionary<Vector3, AStarNode> allNodes, Vector3Int start, Vector3Int end, bool includeStartingPosition)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3? currentPos = end;

        while (currentPos != null && currentPos != start)
        {
            path.Add(currentPos.Value);
            currentPos = allNodes[currentPos.Value].parentPosition;
        }

        if (includeStartingPosition)
        {
            path.Add(start);
        }

        path.Reverse();
        return path;
    }
}
