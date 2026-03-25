using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {

    [Header("Ability Base Configs")]
    [SerializeField] protected GridStateDataSO _gridStateData;
    [SerializeField] protected bool _isFreeAim;

    public bool IsFreeAim => _isFreeAim;
    protected Camera _camera;

    protected virtual void OnEnable()
    {
        _camera = Camera.main;
    }

    public abstract AbilityPreviewData GetPreviewData(Vector3 target, GridElement caster);

    public abstract ICommand CreateCommand(GridElement caster, Vector3 target, List<GridElement> targetsInArea);

    public abstract bool CanExecute(GridElement caster, Vector3 target);

}
