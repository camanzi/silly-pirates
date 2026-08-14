using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridStateData", menuName = "Grid/Grid State Data")]
public class GridStateDataSO : ScriptableObject, ICombatSessionResettable
{
    private Dictionary<Vector3Int, HashSet<GridElement>> _occupiedCells = new();

    private void OnEnable() => _occupiedCells.Clear();

    public void RegisterOccupancy(Vector3Int pos, GridElement entity)
    {
        if (!_occupiedCells.ContainsKey(pos))
            _occupiedCells.Add(pos, new() { entity });
        else
            _occupiedCells[pos].Add(entity);
    }

    public void UnregisterOccupancy(Vector3Int pos, GridElement entity)
    {
        if(_occupiedCells.TryGetValue(pos, out var elements))
        {
            elements.Remove(entity);
            
            if (_occupiedCells[pos].Count == 0)
                _occupiedCells.Remove(pos);
        }
        
    }

    public bool IsOccupied(Vector3Int pos, GridElement entity = null) 
    {
        return _occupiedCells.TryGetValue(pos, out HashSet<GridElement> elementsOnGrid) && (entity == null || elementsOnGrid.Contains(entity));
    }

    public HashSet<GridElement> GetEntityAt(Vector3Int pos)
    {
        _occupiedCells.TryGetValue(pos, out var elements);
        return elements;
    }
    
    public void ClearAll() => _occupiedCells.Clear();

    // Cintura e bretelle: l'unregister per-elemento in GridElement.OnDisable copre lo scarico
    // ordinato della scena, questo copre tutto il resto (elementi distrutti senza passare da
    // OnDisable, occupanti "fantasma" da bug futuri).
    public void ResetForNewCombat() => ClearAll();
}