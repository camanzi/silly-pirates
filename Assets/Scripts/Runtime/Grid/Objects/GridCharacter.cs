using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable
{
    // Da spostare piú avanti in uno SO di stats
    [Header("Grid Character configuration")]
    public int maxMoveSpeed;
    private DirectionalSpriteController _directionalSpriteController;
    private Tween _moveTween;

    protected override void Awake()
    {
        base.Awake();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
    }

    public async Awaitable MoveTo(IEnumerable<Vector3> path, CancellationToken token)
    {
        _moveTween.Stop();
    
        _directionalSpriteController.PlayAnimation("human_walk");

        foreach (Vector3 node in path)
        {
            if (token.IsCancellationRequested) break;

            Vector3 targetPosition = _floorTilemap.GetCellCenterWorld(Vector3Int.FloorToInt(node));

            transform.LookAt(targetPosition);
            await Tween.Position(transform, targetPosition, duration: 0.5f, ease: Ease.Linear);
            
            gridPosition = Vector3Int.FloorToInt(node);
        }

        _directionalSpriteController.PlayAnimation("human_idle");
    }
}
