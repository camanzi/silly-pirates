using PrimeTween;
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

    /// <summary>
    /// Closes the shake telegraph started by <see cref="PartShakeTelegraphCommand"/>: it stops the
    /// infinite tween and gives the loop track back the pivot that had been borrowed from it with
    /// <see cref="CharacterLifecycleAnimator.SuspendLoop"/>.
    ///
    /// It has to be called from EVERY path that turns the shake off — the strike following the telegraph
    /// and the rollback of an interrupted sequence. A teardown on the command cannot be relied on:
    /// <c>ICommand.Undo()</c> is never invoked in this project, so the suspension would stay open forever.
    ///
    /// Restoring the SCALE is left to each caller: the paths legitimately treat it differently (awaited,
    /// fire-and-forget, plain assignment) and it is a channel independent of the loop.
    /// </summary>
    public static void EndPartShake(StepState state, CharacterLifecycleAnimator animator)
    {
        if (state == null) return;

        if (state.Extra.TryGetValue(ShakeTweenKey, out var tObj) && tObj is Tween tween && tween.isAlive)
            tween.Stop();

        // Removed and not merely stopped: the handle is dead, and a second pass (a strike after a partial
        // rollback) must not find a stale Tween to interrogate.
        state.Extra.Remove(ShakeTweenKey);

        animator?.ResumeLoop();
    }
}
