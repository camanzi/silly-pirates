using UnityEngine;

/// <summary>
/// The producer side of the anchor pattern: it publishes a scene instance on a
/// <see cref="RuntimeAnchorSO{T}"/> for as long as it is active. The same shape as SpawnPoint (Register
/// in OnEnable, Unregister in OnDisable), so the lifetime tracks the object's exactly and unloading a
/// scene cleans up after itself.
/// </summary>
public abstract class RuntimeAnchorProvider<TAnchor, TValue> : MonoBehaviour
    where TAnchor : RuntimeAnchorSO<TValue>
    where TValue : Component
{
    [SerializeField] private TAnchor _anchor;

    /// <summary>The value to publish. Called once per enable.</summary>
    protected abstract TValue Resolve();

    // It remembers what was registered: Resolve() may no longer return the same object at teardown time,
    // and an Unregister with the wrong instance would leave the anchor dirty.
    private TValue _registered;

    private void OnEnable()
    {
        if (_anchor == null)
        {
            Debug.LogError($"{GetType().Name}: no anchor assigned.", this);
            return;
        }

        _registered = Resolve();
        if (_registered == null)
        {
            Debug.LogError($"{GetType().Name}: found no {typeof(TValue).Name} to register on '{_anchor.name}'.", this);
            return;
        }

        _anchor.Register(_registered);
    }

    private void OnDisable()
    {
        if (_anchor != null && _registered != null) _anchor.Unregister(_registered);
        _registered = null;
    }
}
