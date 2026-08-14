using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Riferimento a un'istanza di scena risolto a runtime, così che i prefab consumatori non debbano
/// contenere riferimenti cross-oggetto — che dentro un prefab non sono assegnabili — e restino
/// quindi utilizzabili in qualunque scena senza ricablaggi a mano.
///
/// CONTRATTO PER I CONSUMATORI — <i>pull-then-subscribe</i>, sempre in OnEnable:
/// <code>
/// private void OnEnable()
/// {
///     HandleAnchorChanged(_anchor.Value);             // ciò che è GIÀ registrato
///     _anchor.OnValueChanged += HandleAnchorChanged;  // ciò che arriverà DOPO
/// }
/// private void OnDisable() => _anchor.OnValueChanged -= HandleAnchorChanged;
/// </code>
/// La pull copre "produttore già registrato" (stessa scena, o scena additiva caricata prima); la
/// subscribe copre "produttore che arriva dopo" (consumatore nella scena persistente, rig nella
/// scena di combattimento). Un solo pattern copre entrambi gli ordinamenti: non aggiungere
/// risoluzioni lazy nei call site.
///
/// I registrant sono tenuti in una lista e vince il più recente. Serve per lo swap additivo: si
/// carica la scena nuova (il suo produttore si registra) e solo dopo si scarica la vecchia (il suo
/// Unregister diventa un no-op sul valore). In questa finestra due produttori sono legittimamente
/// vivi insieme, e Value non deve mai passare per null.
/// </summary>
public abstract class RuntimeAnchorSO<T> : ScriptableObject where T : Component
{
    private readonly List<T> _registrants = new();

    /// <summary>Il registrant vivo più recente, o null se non ce ne sono.</summary>
    public T Value
    {
        get
        {
            for (int i = _registrants.Count - 1; i >= 0; i--)
                if (_registrants[i] != null)
                    return _registrants[i];
            return null;
        }
    }

    public bool IsSet => Value != null;

    public event Action<T> OnValueChanged;

    // NON è load-bearing per le transizioni di scena. Come SpawnPointManagerSO.OnEnable, scatta una
    // volta per caricamento dell'asset in memoria (avvio app / domain reload), non a ogni load di
    // scena. La correttezza sui load ripetuti viene interamente dal protocollo Register/Unregister:
    // non "semplificare" questa riga pensando che sia lei a reggere le transizioni.
    private void OnEnable() => _registrants.Clear();

    public void Register(T instance)
    {
        if (instance == null || _registrants.Contains(instance)) return;

        WarnIfDuplicateInSameScene(instance);

        T previous = Value;
        _registrants.Add(instance);
        if (Value != previous) OnValueChanged?.Invoke(Value);
    }

    public void Unregister(T instance)
    {
        T previous = Value;
        if (!_registrants.Remove(instance)) return;
        if (Value != previous) OnValueChanged?.Invoke(Value);
    }

    /// <summary>
    /// Due produttori nella STESSA scena sono sempre un errore di authoring (es. due PF_ActionCamera
    /// lasciate attive). Due produttori in scene DIVERSE sono invece la normale finestra di
    /// sovrapposizione di uno swap additivo, e non vanno segnalati: un warning lì sarebbe rumore a
    /// ogni transizione.
    /// </summary>
    private void WarnIfDuplicateInSameScene(T incoming)
    {
        for (int i = 0; i < _registrants.Count; i++)
        {
            T existing = _registrants[i];
            if (existing == null) continue;
            if (existing.gameObject.scene != incoming.gameObject.scene) continue;

            Debug.LogWarning(
                $"[{name}] '{incoming.name}' e '{existing.name}' sono registrati sullo stesso anchor " +
                $"nella stessa scena ('{incoming.gameObject.scene.name}'). L'invariante è un solo " +
                $"produttore attivo: vince il più recente.", incoming);
            return;
        }
    }
}
