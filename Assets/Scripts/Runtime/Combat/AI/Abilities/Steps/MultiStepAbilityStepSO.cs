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
    /// Chiude il telegraph con shake avviato da <see cref="PartShakeTelegraphCommand"/>: ferma il tween
    /// infinito e restituisce al binario loop il pivot che gli era stato tolto in prestito con
    /// <see cref="CharacterLifecycleAnimator.SuspendLoop"/>.
    ///
    /// Va chiamata da OGNI percorso che spegne lo shake — lo strike che segue il telegraph e il rollback di
    /// una sequenza interrotta. Non ci si può appoggiare a un teardown del comando: <c>ICommand.Undo()</c>
    /// non è mai invocato in questo progetto, quindi la sospensione resterebbe aperta per sempre.
    ///
    /// Il ripristino della SCALA resta a ogni chiamante: i percorsi la trattano legittimamente in modo
    /// diverso (atteso, fire-and-forget, assegnazione secca) ed è un canale indipendente dal loop.
    /// </summary>
    public static void EndPartShake(StepState state, CharacterLifecycleAnimator animator)
    {
        if (state == null) return;

        if (state.Extra.TryGetValue(ShakeTweenKey, out var tObj) && tObj is Tween tween && tween.isAlive)
            tween.Stop();

        // Rimossa e non solo fermata: l'handle è morto, e un secondo passaggio (strike dopo un rollback
        // parziale) non deve ritrovarsi un Tween stantio da interrogare.
        state.Extra.Remove(ShakeTweenKey);

        animator?.ResumeLoop();
    }
}
