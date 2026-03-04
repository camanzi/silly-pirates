using System.Collections.Generic;
using UnityEngine;

public interface IAreaShape {
    List<Vector3Int> GetCells(Vector3Int center, int range, Vector3Int casterPos);
}