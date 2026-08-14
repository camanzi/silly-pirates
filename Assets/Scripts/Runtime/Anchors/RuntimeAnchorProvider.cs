using UnityEngine;

/// <summary>
/// Lato produttore del pattern anchor: pubblica un'istanza di scena su un <see cref="RuntimeAnchorSO{T}"/>
/// finché è attiva. Stessa forma di SpawnPoint (Register in OnEnable, Unregister in OnDisable), così
/// il ciclo di vita segue esattamente quello dell'oggetto e lo scarico di una scena si pulisce da sé.
/// </summary>
public abstract class RuntimeAnchorProvider<TAnchor, TValue> : MonoBehaviour
    where TAnchor : RuntimeAnchorSO<TValue>
    where TValue : Component
{
    [SerializeField] private TAnchor _anchor;

    /// <summary>Il valore da pubblicare. Chiamato una volta per abilitazione.</summary>
    protected abstract TValue Resolve();

    // Si ricorda cosa è stato registrato: Resolve() potrebbe non restituire più lo stesso oggetto
    // al momento del teardown, e un Unregister con l'istanza sbagliata lascerebbe l'anchor sporco.
    private TValue _registered;

    private void OnEnable()
    {
        if (_anchor == null)
        {
            Debug.LogError($"{GetType().Name}: nessun anchor assegnato.", this);
            return;
        }

        _registered = Resolve();
        if (_registered == null)
        {
            Debug.LogError($"{GetType().Name}: non ho trovato un {typeof(TValue).Name} da registrare su '{_anchor.name}'.", this);
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
