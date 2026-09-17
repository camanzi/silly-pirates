/// <summary>
/// Marks an object manageable by a <see cref="ComponentPool{T}"/>.
/// Implement it directly only when inheriting from <see cref="PooledBehaviour"/> is not possible.
/// </summary>
public interface IPoolable
{
    /// <summary>The owning pool. Assigned by the pool at creation time, never by the caller.</summary>
    IPoolReleaser Releaser { get; set; }

    /// <summary>True while the object is lent out. Managed by the pool: it is what makes Release() idempotent.</summary>
    bool IsInUse { get; set; }

    /// <summary>Called by the pool when the object is lent out.</summary>
    void OnAcquired();

    /// <summary>Called by the pool when the object comes back. It has to return it to a clean state.</summary>
    void OnReleased();
}
