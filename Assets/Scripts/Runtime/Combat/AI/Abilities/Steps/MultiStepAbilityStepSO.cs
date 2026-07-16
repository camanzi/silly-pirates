using UnityEngine;

public abstract class MultiStepAbilityStepSO : ScriptableObject
{
    [Header("Camera Cue Override")]
    [SerializeField] private bool _overrideCameraCue;
    [SerializeField] private CameraCueType _cameraCueType;
    [SerializeField] private CameraCueProfileSO _cameraCueProfileOverride;

    [Header("Flavor Text Override")]
    [SerializeField][TextArea] private string _flavorTextOverride;

    public CameraCueType? CameraCueTypeOverride => _overrideCameraCue ? _cameraCueType : (CameraCueType?)null;
    public CameraCueProfileSO CameraCueProfileOverride => _cameraCueProfileOverride;
    public string FlavorTextOverride => _flavorTextOverride;

    // previousState may be null (a discard-result scoring pass) or a live StepState to populate in place.
    // Return float.NegativeInfinity if no valid targets/candidates exist.
    public abstract float ComputeScore(AIContext context, StepState previousState, out TargetingData targeting);

    public abstract ICommand CreateCommand(HostileCharacter caster, StepState state);

    // Called on the step that most recently executed when a mid-sequence caster is interrupted
    // (required part broken / caster died / preconditions failed). No-op by default.
    public virtual void OnRolledBack(HostileCharacter caster, StepState state) { }

    public const string RequiredPartTransformKey = "RequiredPartTransform";
    public const string OwningAbilityKey = "OwningAbility";
    public const string ShakeTweenKey = "ShakeTween";
    public const string CasterOriginalScaleKey = "CasterOriginalScale";
    public const string PartOriginalScaleKey = "PartOriginalScale";
    public const string ThreatCellEffectChannelKey = "ThreatCellEffectChannel";
    public const string ThreatKeyKey = "ThreatKey";
}
