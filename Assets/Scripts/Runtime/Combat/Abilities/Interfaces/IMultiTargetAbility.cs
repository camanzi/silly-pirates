/// <summary>Ability che accumula più bersagli prima di eseguirsi.
/// Lo stesso bersaglio può essere scelto più volte (es. 2 colpi su A, 1 su B).</summary>
public interface IMultiTargetAbility
{
    int MaxTargets { get; }
}
