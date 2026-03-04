using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable
{

    private List<Vector3Int> _currentPath;
    private int _currentPathIndex;
    private Tween _moveTween;

    public override void ExecuteAction(IEnumerable<Vector3Int> calculatedPosition)
    {
        base.ExecuteAction(calculatedPosition);
        // MoveTo(calculatedPosition);
    }

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
        //     _moveTween.Stop();
            
        //     _currentPath = (List<Vector3Int>)calculatedPosition;
        //     _currentPathIndex = 0;

        //     FollowNextPathNode();
    }

    // private void FollowNextPathNode()
    // {
    //     if (_currentPath == null || _currentPathIndex >= _currentPath.Count) return;

    //     Vector3Int pathNode = _currentPath[_currentPathIndex];
    //     Vector3 worldPosition = _floorTilemap.CellToWorld(pathNode);

    //     _moveTween = Tween.Position(transform, worldPosition, .5f, Ease.Linear)
    //         .OnComplete(target: this, (t) => {
    //             t.gridPosition = t._currentPath[t._currentPathIndex];
    //             t._currentPathIndex++; 
                
    //             t.FollowNextPathNode();
    //         });
    // }
}
