using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityBase : ScriptableObject
{
    [Header("Dependences")]
    [SerializeField] protected GridStateDataSO _gridStateData;
    [SerializeField] protected SelectionContextSO _selectionCtx;
    
    [Header("Ability Base Configs")]
    [SerializeField] protected bool showTrajectory;
    [SerializeField] protected TrajectoryConfigsSO trajectoryConfigData;

    [Header("UI Rendering")]
    [SerializeField] private Sprite _icon;

    public Sprite Icon => _icon;
    public bool ShowTrajectory => showTrajectory;
    public TrajectoryConfigsSO TrajectoryConfigData => trajectoryConfigData;
    public virtual int ActionPointCost => 0;

    public abstract AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache);

    public abstract ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache);

    public abstract bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache);

    public virtual bool IsPhaseCommand(ICommand command) => false;

    public virtual bool RequiresTargeting => true;

    public virtual float? GetHitChance(IInteractableElement caster, TargetingData targetingData) => null;

}
