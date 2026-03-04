using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IMovable
{
    public Awaitable MoveTo(IEnumerable<Vector3Int> path, CancellationToken token);
}
