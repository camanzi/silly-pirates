using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {

    [SerializeField] protected GridStateDataSO _gridStateData;

    public abstract AbilityPreviewData GetPreviewData(Vector3Int targetCell, GridElement caster);

    public abstract ICommand CreateCommand(GridElement caster, Vector3Int targetCell, List<GridElement> targetsInArea);

    public abstract bool CanExecute(GridElement caster, Vector3Int target);

}
