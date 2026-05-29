using System;
using System.Collections.Generic;
using UnityEngine;

public class PassiveAbilityController : MonoBehaviour
{
    public event Action OnPassivesChanged;
    [Header("Configurazioni")]
    [SerializeField] private List<PassiveAbilitySO> _basePassivesSO;
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;
    private List<PassiveAbilitySO> _instantiatedPassives = new();
    private readonly List<IOnGlobalTurnStart> _globalTurnHandlers = new();

    private void Awake()
    {
        foreach (var passiveSO in _basePassivesSO)
        {
            var instance = Instantiate(passiveSO);
            _instantiatedPassives.Add(instance);
        }
    }

    private void OnEnable()
    {
        foreach (var passive in _instantiatedPassives) passive.OnEquip(this);
        if (_onAnyTurnEnded != null) _onAnyTurnEnded.OnEventRaised += HandleGlobalTurn;
    }

    private void OnDisable()
    {
        foreach (var passive in _instantiatedPassives) passive.OnUnequip(this);
        if (_onAnyTurnEnded != null) _onAnyTurnEnded.OnEventRaised -= HandleGlobalTurn;
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

    public void AddPassive(PassiveAbilitySO instance)
    {
        _instantiatedPassives.Add(instance);
        instance.OnEquip(this);
        OnPassivesChanged?.Invoke();
    }

    public void RemovePassive(PassiveAbilitySO instance)
    {
        instance.OnUnequip(this);
        _instantiatedPassives.Remove(instance);
        Destroy(instance);
        OnPassivesChanged?.Invoke();
    }

    public bool HasPassive<T>() where T : PassiveAbilitySO
    {
        for (int i = 0; i < _instantiatedPassives.Count; i++)
            if (_instantiatedPassives[i] is T) return true;
        return false;
    }

    private void HandleGlobalTurn(ITurnAgent _)
    {
        GetModifiers(_globalTurnHandlers);
        foreach (var h in _globalTurnHandlers) h.OnGlobalTurnStart();
    }
}
