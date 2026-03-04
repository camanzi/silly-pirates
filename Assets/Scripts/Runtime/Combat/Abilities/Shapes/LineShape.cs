using System.Collections.Generic;
using UnityEngine;

public class LineShape : IAreaShape
{
    public List<Vector3Int> GetCells(Vector3Int center, int range, Vector3Int casterPos)
    {
        Debug.Log($"Getting shape Line from position {center} of range {range}");
        return new List<Vector3Int>();
    }
}
