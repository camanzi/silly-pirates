using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {

    [SerializeField] protected GridStateDataSO _gridStateData;

    protected Camera _camera;

    protected virtual void OnEnable()
    {
        _camera = Camera.main;
    }

    public abstract AbilityPreviewData GetPreviewData(Vector3 target, GridElement caster);

    public abstract ICommand CreateCommand(GridElement caster, Vector3Int targetCell, List<GridElement> targetsInArea);

    public abstract bool CanExecute(GridElement caster, Vector3Int target);

}
