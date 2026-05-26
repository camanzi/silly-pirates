using UnityEngine;

public abstract class EnemyAbilityBase : AbilityBase
{
    [SerializeField] private EnemyPartSO _requiredPart;

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
        => AbilityPreviewData.Empty;

    /// <summary>
    /// Returns float.NegativeInfinity if preconditions fail or no valid target exists.
    /// Populates 'targeting' with the chosen target and data.
    /// </summary>
    public float Score(AIContext context, out TargetingData targeting)
    {
        if (!MeetsPreconditions(context)) { targeting = default; return float.NegativeInfinity; }
        return ComputeScore(context, out targeting);
    }

    protected virtual bool MeetsPreconditions(AIContext context)
    {
        if (_requiredPart == null) return true;
        return (context.Caster as IPartOwner)?.IsPartFunctional(_requiredPart) ?? true;
    }

    protected abstract float ComputeScore(AIContext context, out TargetingData targeting);
}
