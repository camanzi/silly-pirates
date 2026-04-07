using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {

    [Header("Dependences")]
    [SerializeField] protected GridStateDataSO _gridStateData;
    [SerializeField] protected SelectionContextSO _selectionCtx;
    
    [Header("Ability Base Configs")]
    [SerializeField] protected bool showTrajectory;

    public bool ShowTrajectory => showTrajectory;
    protected Camera _camera;

    protected virtual void OnEnable()
    {
        _camera = Camera.main;
    }

    public abstract AbilityPreviewData GetPreviewData(TargetingData targetingData, IInteractableElement caster);

    public abstract ICommand CreateCommand(IInteractableElement caster, TargetingData targetingData);

    public abstract bool CanExecute(IInteractableElement caster, TargetingData targetingData);

}
