using System.Collections.Generic;
using System.Linq;
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
        return _instantiatedPassives.OfType<T>();
    }
}
