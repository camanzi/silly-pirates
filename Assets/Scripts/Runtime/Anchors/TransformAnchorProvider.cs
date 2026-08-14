using UnityEngine;

/// <summary>Pubblica il proprio Transform: usato dai bersagli di camera (target group, free-roam target, punto di mira sulla nave).</summary>
public class TransformAnchorProvider : RuntimeAnchorProvider<TransformAnchorSO, Transform>
{
    protected override Transform Resolve() => transform;
}
