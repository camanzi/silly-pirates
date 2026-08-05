using System;
using System.Collections.Generic;
using UnityEngine;

public class PassiveAbilityController : MonoBehaviour
{
    public event Action OnPassivesChanged;
    [Header("Configurazioni")]
    [SerializeField] private List<PassiveAbilitySO> _basePassivesSO;
    [SerializeField] private PassiveNotificationEventChannel _passiveNotificationChannel;

    [Header("Native Passive")]
    [SerializeField] private PassiveAbilitySO _nativePassiveSO;
    private PassiveAbilitySO _nativePassiveInstance;
    public PassiveAbilitySO NativePassive => _nativePassiveInstance;

    private List<PassiveAbilitySO> _instantiatedPassives = new();
    private readonly List<IOnGlobalTurnStart> _globalTurnHandlers    = new();
    private readonly List<IOnGlobalTurnEnd>   _globalTurnEndHandlers = new();
    private readonly List<IOnTurnEnd>         _turnEndHandlers       = new();

    public Collider Collider { get; private set; }

    private void Awake()
    {
        Collider = GetComponent<Collider>();

        foreach (var passiveSO in _basePassivesSO)
        {
            var instance = Instantiate(passiveSO);
            _instantiatedPassives.Add(instance);
        }

        if (_nativePassiveSO != null)
        {
            _nativePassiveInstance = Instantiate(_nativePassiveSO);
            _instantiatedPassives.Add(_nativePassiveInstance);
        }
    }

    private void OnEnable()
    {
        foreach (var passive in _instantiatedPassives) passive.OnEquip(this);
    }

    private void OnDisable()
    {
        foreach (var passive in _instantiatedPassives) passive.OnUnequip(this);
    }

    public IEnumerable<T> GetModifiers<T>()
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
            if (_instantiatedPassives[i] is T t) yield return t;
    }

    public void GetModifiers<T>(List<T> results)
    {
        results.Clear();
        for (int i = 0; i < _instantiatedPassives.Count; i++)
            if (_instantiatedPassives[i] is T t) results.Add(t);
    }

    public PassiveAbilitySO AddPassive(PassiveAbilitySO instance)
    {
        var type = instance.GetType();
        for (int i = 0; i < _instantiatedPassives.Count; i++)
        {
            if (_instantiatedPassives[i].GetType() != type) continue;

            var existing = _instantiatedPassives[i];
            if (existing is IStackablePassive stackable)
                stackable.OnReapplied(this);
            else
                Debug.LogWarning($"[PassiveAbilityController] {name} already has passive '{instance.DisplayName}' ({type.Name}); duplicate application ignored.");

            Destroy(instance);
            OnPassivesChanged?.Invoke();
            return existing;
        }

        _instantiatedPassives.Add(instance);
        instance.OnEquip(this);
        OnPassivesChanged?.Invoke();
        _passiveNotificationChannel?.RaiseEvent(new PassiveNotificationEvent
        {
            DisplayName = instance.DisplayName,
            WasAdded = true,
            WorldPosition = transform.position,
            Source = transform,
        });
        return instance;
    }

    public void RemovePassive(PassiveAbilitySO instance)
    {
        instance.OnUnequip(this);
        _instantiatedPassives.Remove(instance);
        _passiveNotificationChannel?.RaiseEvent(new PassiveNotificationEvent
        {
            DisplayName = instance.DisplayName,
            WasAdded = false,
            WorldPosition = transform.position,
            Source = transform,
        });
        Destroy(instance);
        OnPassivesChanged?.Invoke();
    }

    public bool HasPassive<T>() where T : PassiveAbilitySO
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
            if (_instantiatedPassives[i] is T) return true;
        return false;
    }

    public bool TryGetPassive<T>(out T passive) where T : PassiveAbilitySO
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
        {
            if (_instantiatedPassives[i] is T t) { passive = t; return true; }
        }
        passive = null;
        return false;
    }

    public bool RemovePassive<T>() where T : PassiveAbilitySO
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
        {
            if (_instantiatedPassives[i] is T t)
            {
                RemovePassive(t);
                return true;
            }
        }
        return false;
    }

    public bool RemovePassiveOfType<TMarker>() where TMarker : class
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
        {
            if (_instantiatedPassives[i] is TMarker)
            {
                RemovePassive(_instantiatedPassives[i]);
                return true;
            }
        }
        return false;
    }

    public void HandleGlobalTurnStart()
    {
        GetModifiers(_globalTurnHandlers);
        foreach (var h in _globalTurnHandlers) h.OnGlobalTurnStart();
    }

    public void HandleGlobalTurnEnd()
    {
        GetModifiers(_globalTurnEndHandlers);
        foreach (var h in _globalTurnEndHandlers) h.OnGlobalTurnEnd();
    }

    public void HandleOwnerTurnEnd()
    {
        GetModifiers(_turnEndHandlers);
        foreach (var h in _turnEndHandlers) h.OnTurnEnd();
    }
}
