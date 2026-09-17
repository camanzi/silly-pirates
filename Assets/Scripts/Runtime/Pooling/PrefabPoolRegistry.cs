using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of <see cref="ComponentPool{T}"/>, one per prefab. It serves the domains where there is no
/// single prefab but dozens of them (the VFX): the right pool is created on the first Acquire and reused
/// from then on.
///
/// It adds no policy of its own: it propagates the exhaustion null exactly as ComponentPool does, and
/// leaves the decision on what to do to the consumer.
/// </summary>
public class PrefabPoolRegistry<T> where T : Component, IPoolable
{
    private readonly Dictionary<T, ComponentPool<T>> _poolsByPrefab = new();
    private readonly Transform _root;
    private readonly int _prewarmPerPrefab;
    private readonly int _maxPerPrefab;

    public PrefabPoolRegistry(Transform root, int prewarmPerPrefab, int maxPerPrefab)
    {
        _root = root;
        _prewarmPerPrefab = prewarmPerPrefab;
        _maxPerPrefab = maxPerPrefab;
    }

    public int PoolCount => _poolsByPrefab.Count;

    /// <summary>Lends out an instance of the requested prefab. Null when that pool is exhausted.</summary>
    public T Acquire(T prefab)
    {
        if (prefab == null) return null;

        if (!_poolsByPrefab.TryGetValue(prefab, out ComponentPool<T> pool))
        {
            pool = CreatePool(prefab);
            _poolsByPrefab[prefab] = pool;
        }

        return pool.Acquire();
    }

    /// <summary>
    /// Returns an instance to the pool that created it. There is no need to know which prefab it came
    /// from: the reference to the pool is already on <see cref="IPoolable.Releaser"/>.
    /// </summary>
    public void Release(T instance)
    {
        if (instance == null) return;
        instance.Releaser?.Release(instance);
    }

    /// <summary>To be called in the owner's OnDisable.</summary>
    public void ReleaseAll()
    {
        foreach (ComponentPool<T> pool in _poolsByPrefab.Values)
            pool.ReleaseAll();
    }

    /// <summary>To be called in the owner's OnDestroy.</summary>
    public void Clear()
    {
        foreach (ComponentPool<T> pool in _poolsByPrefab.Values)
            pool.Clear();

        _poolsByPrefab.Clear();
    }

    /// <summary>
    /// Each prefab gets its own sub-root: keeping the instances grouped by type keeps the hierarchy
    /// readable in the Inspector while reuse is being checked.
    /// </summary>
    private ComponentPool<T> CreatePool(T prefab)
    {
        Transform poolRoot = _root;

        if (_root != null)
        {
            var go = new GameObject($"Pool_{prefab.name}");
            go.transform.SetParent(_root, false);
            poolRoot = go.transform;
        }

        return new ComponentPool<T>(prefab, poolRoot, _prewarmPerPrefab, _maxPerPrefab);
    }
}
