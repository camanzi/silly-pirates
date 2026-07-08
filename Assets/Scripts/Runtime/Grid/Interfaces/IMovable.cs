using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IMovable
{
    public int RemainingMovementPoints { get; set; }

    public IntEventChannel OnMovementPointsChanged { get; }

    public Awaitable MoveTo(IEnumerable<Vector3> path, CancellationToken token);
    public int EvaluateMovementBonus();
}
