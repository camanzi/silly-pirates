using System.Collections.Generic;
using UnityEngine;

public struct CellEffectPayload
{
    public string Key;
    public List<Vector3Int> Cells;
    public Material Material;
}
