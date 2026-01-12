using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class GridUtils
{
    /**
     * Find the inner area based on the cellBounds
     * 
     * param name="tilemap" the tilemap on wich calculate the inner area
     * param name="startingPos" the starting position for flood fill algorithm
     * 
     * returns HashMap with all the cells strictly inside the borders 
     */
    public static HashSet<Vector3Int> FindInnerArea(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        bool[,] occupied = new bool[bounds.size.x, bounds.size.y];
        bool[,] border = new bool[bounds.size.x, bounds.size.y];

        // Initialize occupied matrix
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int pos = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                occupied[x, y] = tilemap.HasTile(pos);
            }
        }

        // Initialize border matrix
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                if (occupied[x, y] && IsBound(bounds, new Vector2Int(x, y), occupied))
                {
                    border[x, y] = true;
                }
            }
        }

        return FloodAlgorithm(FindFirstFreeTileInBound(occupied, bounds), bounds, border);
    }


    /**
     * Flood an ipotetical tilemap having the borders matrix and the bounderies
     * 
     * param name="startingPos" the starting flood position
     * param name="bounds" bounderies for the flood algorith to operate
     * param name="borders" tilemap borders, (x, y) = true is border and will stop the flood
     * 
     * returns HashMap with all the cells strictly inside the borders 
     */
    public static HashSet<Vector3Int> FloodAlgorithm(Vector3Int startingPos, BoundsInt bounds, bool[,] border) 
    {
        HashSet<Vector3Int> innerArea = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        int seedX = startingPos.x - bounds.x;
        int seedY = startingPos.y - bounds.y;

        if (seedX < 0 || seedX >= bounds.size.x || seedY < 0 || seedY >= bounds.size.y)
        {
            Debug.LogError("Seed position fuori dai bounds della tilemap!");
            return innerArea;
        }

        if (border[seedX, seedY])
        {
            Debug.LogError("Seed position non valida: NON pu� essere un bordo.");
            return innerArea;
        }

        queue.Enqueue(startingPos);
        innerArea.Add(startingPos);

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1,0,0),
            new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,-1,0)
        };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;
                int nx = neighbor.x - bounds.x;
                int ny = neighbor.y - bounds.y;

                if (nx >= 0 && nx < bounds.size.x && ny >= 0 && ny < bounds.size.y)
                {
                    if (!border[nx, ny] && !innerArea.Contains(neighbor))
                    {
                        innerArea.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return innerArea;
    }

    public static Vector3Int FindFirstFreeTileInBound(bool[,] occupied, BoundsInt bounds) 
    {
        for (int i = 0; i < occupied.GetLength(0); i++) 
        {
            for (int j = 0; j < occupied.GetLength(1); j++) 
            {
                if (!occupied[i, j]) 
                {
                    // Debug.Log($"First tile found ({bounds.x + i}, {bounds.y + j})");
                    return new Vector3Int(bounds.x + i, bounds.y + j, 0);
                }
            }
        }

        Debug.LogError("Cannot find any free tile");

        return default(Vector3Int);
    }

    public static bool IsBound(BoundsInt bounds, Vector2Int tileCoord, bool[,] occupied) 
    {
        return 
            tileCoord.x == 0 || !occupied[tileCoord.x - 1, tileCoord.y] || tileCoord.x == bounds.size.x - 1 || !occupied[tileCoord.x + 1, tileCoord.y] 
            || tileCoord.y == 0 || !occupied[tileCoord.x, tileCoord.y - 1] || tileCoord.y == bounds.size.y - 1 || !occupied[tileCoord.x, tileCoord.y + 1];
    }
}
