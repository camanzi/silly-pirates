using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Anima l'ingresso in scena della nave durante la sequenza di intro al combattimento.
/// Lascia <see cref="ShipController"/> intatto: resta responsabile solo delle tilemap.
/// </summary>
public class ShipEntranceAnimator : MonoBehaviour
{
    [Tooltip("Offset (relativo alla posizione di attracco) da cui la nave parte, es. da sinistra dello schermo")]
    [SerializeField] private Vector3 _entryOffset = new(-40f, 0f, 0f);
    [SerializeField] private float _duration = 3.5f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    /// <summary>Posizione autorale della nave in scena, catturata da <see cref="SnapToEntry"/>.</summary>
    public Vector3 DockPosition { get; private set; }

    /// <summary>
    /// Cattura la posizione autorale come punto di attracco e sposta subito la nave al punto di
    /// ingresso. Va chiamato DOPO che i <see cref="GridElement"/> figli hanno risolto la propria
    /// tilemap (quindi da uno <c>Start</c>, non da un <c>Awake</c>): il loro raycast di
    /// inizializzazione deve avvenire con la nave alla posizione di attracco.
    /// </summary>
    public void SnapToEntry()
    {
        DockPosition = transform.position;
        transform.position = DockPosition + _entryOffset;

        // Il progetto ha m_AutoSyncTransforms: 0 (ProjectSettings/DynamicsManager.asset): senza
        // questa chiamata i collider della nave resterebbero all'attracco fino al prossimo step di
        // simulazione, e ogni query fisica dello stesso frame (es. l'OverlapSphere di
        // SpawnPoint.Start) leggerebbe posizioni obsolete.
        Physics.SyncTransforms();
    }

    public async Awaitable ArriveAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Tween.Position(transform, DockPosition, _duration, _ease);
    }
}
