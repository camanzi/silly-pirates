using UnityEngine;

public interface ICellCostModifier
{
    int GetAdditionalCost(Vector3Int cell);
}
