using UnityEngine;

/// <summary>
/// Riferimento non generico a una pool. Permette a un oggetto poolable di restituirsi
/// da solo senza conoscere il tipo concreto della pool che lo possiede.
/// </summary>
public interface IPoolReleaser
{
    void Release(Component instance);
}
