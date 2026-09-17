using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A generic pool of poolable Components, agnostic to the domain: it can create, lend, take back and
/// count. It applies no policy when exhausted — Acquire() returns null and the decision (steal, drop,
/// log) is left to the consumer, which can inspect Active.
///
/// Objects stay parented to the pool's root ALWAYS: they must never be reparented onto gameplay objects,
/// or their destruction would shrink the pool permanently.
/// </summary>
public class ComponentPool<T> : IPoolReleaser where T : Component, IPoolable
{
    private readonly List<T> _free = new();
    private readonly List<T> _active = new();
    private readonly Func<T> _factory;
    private readonly Transform _root;
    private readonly int _maxSize;

    private int _totalCount;

    /// <summary>A pool fed by a prefab.</summary>
    public ComponentPool(T prefab, Transform root, int prewarm, int maxSize)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        _factory = () => UnityEngine.Object.Instantiate(prefab);
        _root = root;
        _maxSize = Mathf.Max(1, maxSize);
        Prewarm(prewarm);
    }

    /// <summary>A pool fed by a factory, for objects built in code without a prefab.</summary>
    public ComponentPool(Func<T> factory, Transform root, int prewarm, int maxSize)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _root = root;
        _maxSize = Mathf.Max(1, maxSize);
        Prewarm(prewarm);
    }

    /// <summary>The objects currently lent out. Read-only: the consumer needs it for its own policies.</summary>
    public IReadOnlyList<T> Active => _active;

    public int TotalCount => _totalCount;
    public int FreeCount => _free.Count;
    public int ActiveCount => _active.Count;
    public int MaxSize => _maxSize;

    /// <summary>True when there are no free objects left and no more can be created.</summary>
    public bool IsExhausted => _free.Count == 0 && _totalCount >= _maxSize;

    /// <summary>
    /// Lends out a free object, creating a new one while still under the cap.
    /// It returns null when the pool is exhausted: that is not an error, it is the signal that the caller
    /// has to apply its own policy.
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

    /// <summary>Returns an object to the pool. Idempotent: a second Release is a no-op.</summary>
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

    /// <summary>Brings every lent-out object back into the pool. To be called in the owner's OnDisable.</summary>
    public void ReleaseAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            Release(_active[i]);
    }

    /// <summary>Destroys every object the pool created. To be called in the owner's OnDestroy.</summary>
    public void Clear()
    {
        ReleaseAll();

        for (int i = 0; i < _free.Count; i++)
        {
            T instance = _free[i];
            if (instance == null) continue;

            // Clear() comes from OnDestroy, which also runs outside play mode.
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

            // The object was destroyed from outside: it is dropped and the next one is tried.
            _totalCount--;
        }

        return null;
    }

    private T CreateInstance()
    {
        T instance = _factory();

        if (instance == null)
        {
            Debug.LogError($"[ComponentPool<{typeof(T).Name}>] The factory returned null.");
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
