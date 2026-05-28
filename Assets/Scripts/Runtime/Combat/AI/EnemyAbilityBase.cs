using UnityEngine;

public abstract class EnemyAbilityBase : AbilityBase
{
    [Header("Composite enemy configuration")]
    [SerializeField] private EnemyPartSO _requiredPart;

    [Header("AI Scoring")]
    [Tooltip("Base priority of this ability. Higher values make it more likely to be chosen over other abilities, all else equal.")]
    [SerializeField] private float _basePriority = 0f;

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache) => AbilityPreviewData.Empty;

    /// <summary>
    /// Returns float.NegativeInfinity if preconditions fail or no valid target exists.
    /// Populates 'targeting' with the chosen target and data.
    /// </summary>
    public float Score(AIContext context, out TargetingData targeting)
    {
        if (!MeetsPreconditions(context)) { targeting = default; return float.NegativeInfinity; }
        float raw = ComputeScore(context, out targeting);
        if (raw == float.NegativeInfinity) return float.NegativeInfinity;
        return _basePriority + raw;
    }

    protected virtual bool MeetsPreconditions(AIContext context)
    {
        if (_requiredPart == null) return true;
        return (context.Caster as IPartOwner)?.IsPartFunctional(_requiredPart) ?? true;
    }

    protected abstract float ComputeScore(AIContext context, out TargetingData targeting);
}
