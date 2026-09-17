using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Animates the ship's entrance into the scene during the combat intro sequence.
/// It leaves <see cref="ShipController"/> untouched: that stays responsible for the tilemaps only.
/// </summary>
public class ShipEntranceAnimator : MonoBehaviour
{
    [Tooltip("Offset (relative to the docking position) the ship starts from, e.g. off the left of the screen")]
    [SerializeField] private Vector3 _entryOffset = new(-40f, 0f, 0f);
    [SerializeField] private float _duration = 3.5f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    /// <summary>The ship's authored position in the scene, captured by <see cref="SnapToEntry"/>.</summary>
    public Vector3 DockPosition { get; private set; }

    /// <summary>
    /// Captures the authored position as the docking point and moves the ship straight to the entry
    /// point. It has to be called AFTER the child <see cref="GridElement"/> instances have resolved their
    /// own tilemap (so from a <c>Start</c>, not from an <c>Awake</c>): their initialization raycast has to
    /// happen with the ship at its docking position.
    /// </summary>
    public void SnapToEntry()
    {
        DockPosition = transform.position;
        transform.position = DockPosition + _entryOffset;

        // The project has m_AutoSyncTransforms: 0 (ProjectSettings/DynamicsManager.asset): without this
        // call the ship's colliders would stay at the dock until the next simulation step, and every
        // physics query in the same frame (e.g. SpawnPoint.Start's OverlapSphere) would read stale
        // positions.
        Physics.SyncTransforms();
    }

    public async Awaitable ArriveAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Tween.Position(transform, DockPosition, _duration, _ease);
    }
}
