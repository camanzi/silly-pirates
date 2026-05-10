using System.Collections.Generic;
using UnityEngine;

public class PassiveAbilityController : MonoBehaviour
{
    [Header("Configurazioni")]
    [SerializeField] private List<PassiveAbilitySO> _basePassivesSO;
    private List<PassiveAbilitySO> _instantiatedPassives = new();
        
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
}
