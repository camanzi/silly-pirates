using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {
    public abstract List<Vector3Int> GetAffectedCells(Vector3Int target, GridElement caster);

    public abstract ICommand CreateCommand(GridElement caster, Vector3Int target, List<GridElement> targetsInArea);
    
    public abstract bool CanExecute(GridElement caster, Vector3Int target);
}
