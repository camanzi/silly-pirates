/// <summary>An ability that collects several targets before executing.
/// The same target can be picked more than once (e.g. 2 shots on A, 1 on B).</summary>
public interface IMultiTargetAbility
{
    int MaxTargets { get; }
}
