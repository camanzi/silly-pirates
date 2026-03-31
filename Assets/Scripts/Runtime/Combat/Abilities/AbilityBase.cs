using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject {

    [Header("Ability Base Configs")]
    [SerializeField] protected GridStateDataSO _gridStateData;
    [SerializeField] protected bool _isFreeAim;

    public bool isFreeAim => _isFreeAim;
    protected Camera _camera;

    protected virtual void OnEnable()
    {
        _camera = Camera.main;
    }

    public abstract AbilityPreviewData GetPreviewData(Vector3 target, IInteractableElement caster);

    public abstract ICommand CreateCommand(IInteractableElement caster, Vector3 target, List<IInteractableElement> targetsInArea);

    public abstract bool CanExecute(IInteractableElement caster, Vector3 target);

}
