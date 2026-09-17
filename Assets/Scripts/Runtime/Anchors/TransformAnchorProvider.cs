using UnityEngine;

/// <summary>Publishes its own Transform: used by the camera targets (target group, free-roam target, the aim point on the ship).</summary>
public class TransformAnchorProvider : RuntimeAnchorProvider<TransformAnchorSO, Transform>
{
    protected override Transform Resolve() => transform;
}
