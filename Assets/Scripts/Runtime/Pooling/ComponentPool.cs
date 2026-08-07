using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool generica di Component poolable, agnostica rispetto al dominio: sa creare, prestare,
/// riprendere e contare. Non applica nessuna policy quando e' esaurita — Acquire() ritorna null
/// e la decisione (rubare, scartare, loggare) resta al consumatore, che puo' ispezionare Active.
///
/// Gli oggetti restano SEMPRE parentati al root della pool: non vanno mai riparentati su oggetti
/// di gameplay, altrimenti la loro distruzione rimpicciolirebbe la pool in modo permanente.
/// </summary>
public class ComponentPool<T> : IPoolReleaser where T : Component, IPoolable
{
    private readonly List<T> _free = new();
    private readonly List<T> _active = new();
    private readonly Func<T> _factory;
    private readonly Transform _root;
    private readonly int _maxSize;

    private int _totalCount;

    /// <summary>Pool alimentata da un prefab.</summary>
    public ComponentPool(T prefab, Transform root, int prewarm, int maxSize)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        _factory = () => UnityEngine.Object.Instantiate(prefab);
        _root = root;
        _maxSize = Mathf.Max(1, maxSize);
        Prewarm(prewarm);
    }

    /// <summary>Pool alimentata da una factory, per oggetti costruiti via codice senza prefab.</summary>
    public ComponentPool(Func<T> factory, Transform root, int prewarm, int maxSize)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _root = root;
        _maxSize = Mathf.Max(1, maxSize);
        Prewarm(prewarm);
    }

    /// <summary>Oggetti attualmente prestati. Sola lettura: serve al consumatore per le sue policy.</summary>
    public IReadOnlyList<T> Active => _active;

    public int TotalCount => _totalCount;
    public int FreeCount => _free.Count;
    public int ActiveCount => _active.Count;
    public int MaxSize => _maxSize;

    /// <summary>True quando non ci sono oggetti liberi e non se ne possono piu' creare.</summary>
    public bool IsExhausted => _free.Count == 0 && _totalCount >= _maxSize;

    /// <summary>
    /// Presta un oggetto libero, creandone uno nuovo se si e' sotto il cap.
    /// Ritorna null quando la pool e' esaurita: non e' un errore, e' il segnale
    /// che il chiamante deve applicare la propria policy.
    /// </summary>
    public T Acquire()
    {
        T instance = TakeFree();

        if (instance == null)
        {
            if (_totalCount >= _maxSize) return null;
            instance = CreateInstance();
            if (instance == null) return null;
        }

        instance.IsInUse = true;
        _active.Add(instance);
        instance.OnAcquired();
        return instance;
    }

    /// <summary>Restituisce un oggetto alla pool. Idempotente: un secondo Release e' un no-op.</summary>
    public void Release(T instance)
    {
        if (instance == null) return;
        if (!instance.IsInUse) return;

        instance.IsInUse = false;

        int index = _active.IndexOf(instance);
        if (index >= 0) _active.RemoveAt(index);

        instance.OnReleased();

        if (_root != null && instance.transform.parent != _root)
            instance.transform.SetParent(_root, false);

        _free.Add(instance);
    }

    void IPoolReleaser.Release(Component instance)
    {
        if (instance is T typed) Release(typed);
    }

    /// <summary>Riporta tutti gli oggetti prestati nella pool. Da chiamare in OnDisable del proprietario.</summary>
    public void ReleaseAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            Release(_active[i]);
    }

    /// <summary>Distrugge ogni oggetto creato dalla pool. Da chiamare in OnDestroy del proprietario.</summary>
    public void Clear()
    {
        ReleaseAll();

        for (int i = 0; i < _free.Count; i++)
        {
            T instance = _free[i];
            if (instance == null) continue;

            // Clear() arriva da OnDestroy, che gira anche fuori dal play mode.
            if (Application.isPlaying) UnityEngine.Object.Destroy(instance.gameObject);
            else UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }

        _free.Clear();
        _active.Clear();
        _totalCount = 0;
    }

    private T TakeFree()
    {
        while (_free.Count > 0)
        {
            int last = _free.Count - 1;
            T candidate = _free[last];
            _free.RemoveAt(last);

            if (candidate != null) return candidate;

            // L'oggetto e' stato distrutto dall'esterno: lo si scarta e si prova il successivo.
            _totalCount--;
        }

        return null;
    }

    private T CreateInstance()
    {
        T instance = _factory();

        if (instance == null)
        {
            Debug.LogError($"[ComponentPool<{typeof(T).Name}>] La factory ha restituito null.");
            return null;
        }

        if (_root != null) instance.transform.SetParent(_root, false);

        instance.Releaser = this;
        instance.IsInUse = false;
        _totalCount++;
        return instance;
    }

    private void Prewarm(int count)
    {
        int target = Mathf.Clamp(count, 0, _maxSize);

        for (int i = 0; i < target; i++)
        {
            T instance = CreateInstance();
            if (instance == null) return;

            instance.OnReleased();
            _free.Add(instance);
        }
    }
}
