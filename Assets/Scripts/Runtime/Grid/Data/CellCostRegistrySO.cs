using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cell Cost Registry", menuName = "Grid/Cell Cost Registry")]
public class CellCostRegistrySO : ScriptableObject
{
    private readonly List<ICellCostModifier> _modifiers = new();

    public void Register(ICellCostModifier modifier) => _modifiers.Add(modifier);

    public void Unregister(ICellCostModifier modifier) => _modifiers.Remove(modifier);

    public int GetMovementCost(Vector3Int cell)
    {
        int cost = 1;
        for (int i = 0; i < _modifiers.Count; i++)
            cost += _modifiers[i].GetAdditionalCost(cell);
        return cost;
    }
}
