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

    [Header("Camera Direction")]
    [SerializeField] private CameraCueType _cameraCue = CameraCueType.FramePair;
    [SerializeField] private CameraCueProfileSO _cameraCueProfile;

    [Header("Audio")]
    [Tooltip("Serve solo alle abilita' che passano il canale ai propri comandi per SFX a tempo preciso")]
    [SerializeField] protected SfxCueEventChannel _sfxChannel;
    [Tooltip("Suono generico di lancio, riprodotto all'ingresso nello stato di esecuzione")]
    [SerializeField] private SoundEventSO _castSfx;

    public Sprite Icon => _icon;
    public virtual CameraCueType CameraCue => _cameraCue;
    public virtual CameraCueProfileSO CameraCueProfile => _cameraCueProfile;
    public virtual SoundEventSO CastSfx => _castSfx;
    public SfxCueEventChannel SfxChannel => _sfxChannel;
    public bool ShowTrajectory => showTrajectory;
    public TrajectoryConfigsSO TrajectoryConfigData => trajectoryConfigData;
    public virtual int ActionPointCost => 0;
    public virtual int GetMovementPointCost(AbilityPreviewData previewData, ref object cache) => 0;

    public abstract AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache);

    public abstract ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache);

    public abstract bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache);

    public virtual bool IsValidTarget(IInteractableElement caster, TargetingData? targetingData, ref object cache) => true;

    public virtual bool IsPhaseCommand(ICommand command) => false;

    public virtual bool RequiresTargeting => true;

    public virtual float? GetHitChance(IInteractableElement caster, TargetingData targetingData) => null;

    public virtual void OnTargetingExit(IInteractableElement caster, ref object cache) { }

}
