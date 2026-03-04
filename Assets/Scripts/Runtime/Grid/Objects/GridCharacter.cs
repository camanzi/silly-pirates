using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable
{
    private Tween _moveTween;

    public async Awaitable MoveTo(IEnumerable<Vector3Int> path, CancellationToken token)
    {
        _moveTween.Stop();
    
        foreach (Vector3Int node in path)
        {
            if (token.IsCancellationRequested) break;

            Vector3 worldPosition = _floorTilemap.GetCellCenterWorld(node);

            await Tween.Position(transform, worldPosition, duration: 0.5f, ease: Ease.Linear);
            
            gridPosition = node;
        }
    }
}
