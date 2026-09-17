using UnityEngine;

/// <summary>
/// A non-generic reference to a pool. It lets a poolable object return itself without knowing the
/// concrete type of the pool that owns it.
/// </summary>
public interface IPoolReleaser
{
    void Release(Component instance);
}
