using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Insieme di <see cref="ComponentPool{T}"/>, una per prefab. Serve ai domini in cui non esiste
/// "il" prefab ma decine (i VFX): la pool giusta viene creata al primo Acquire e poi riusata.
///
/// Non aggiunge nessuna policy: propaga il null di esaurimento esattamente come ComponentPool,
/// e lascia al consumatore la decisione su cosa fare.
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

    /// <summary>Presta un'istanza del prefab richiesto. Null se quella pool e' esaurita.</summary>
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
    /// Restituisce un'istanza alla pool che l'ha creata. Non serve sapere da quale prefab
    /// provenga: il riferimento alla pool e' gia' su <see cref="IPoolable.Releaser"/>.
    /// </summary>
    public void Release(T instance)
    {
        if (instance == null) return;
        instance.Releaser?.Release(instance);
    }

    /// <summary>Da chiamare in OnDisable del proprietario.</summary>
    public void ReleaseAll()
    {
        foreach (ComponentPool<T> pool in _poolsByPrefab.Values)
            pool.ReleaseAll();
    }

    /// <summary>Da chiamare in OnDestroy del proprietario.</summary>
    public void Clear()
    {
        foreach (ComponentPool<T> pool in _poolsByPrefab.Values)
            pool.Clear();

        _poolsByPrefab.Clear();
    }

    /// <summary>
    /// Ogni prefab ha il suo sotto-root: tenere le istanze raggruppate per tipo rende
    /// leggibile la gerarchia in Inspector mentre si verifica il riuso.
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
