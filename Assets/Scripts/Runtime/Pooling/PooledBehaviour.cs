using UnityEngine;

/// <summary>
/// The base for poolable MonoBehaviours: it removes <see cref="IPoolable"/>'s boilerplate.
/// Subclasses override OnAcquired/OnReleased, always calling base.
/// </summary>
public abstract class PooledBehaviour : MonoBehaviour, IPoolable
{
    public IPoolReleaser Releaser { get; set; }
    public bool IsInUse { get; set; }

    public virtual void OnAcquired() => gameObject.SetActive(true);

    public virtual void OnReleased() => gameObject.SetActive(false);

    /// <summary>Self-return to the pool, usable by the object itself once its work is done.</summary>
    protected void ReleaseSelf() => Releaser?.Release(this);
}
