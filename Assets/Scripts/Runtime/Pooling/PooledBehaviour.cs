using UnityEngine;

/// <summary>
/// Base per i MonoBehaviour poolable: azzera il boilerplate di <see cref="IPoolable"/>.
/// Le sottoclassi fanno override di OnAcquired/OnReleased chiamando sempre base.
/// </summary>
public abstract class PooledBehaviour : MonoBehaviour, IPoolable
{
    public IPoolReleaser Releaser { get; set; }
    public bool IsInUse { get; set; }

    public virtual void OnAcquired() => gameObject.SetActive(true);

    public virtual void OnReleased() => gameObject.SetActive(false);

    /// <summary>Auto-restituzione alla pool, usabile dall'oggetto stesso quando ha finito il suo lavoro.</summary>
    protected void ReleaseSelf() => Releaser?.Release(this);
}
