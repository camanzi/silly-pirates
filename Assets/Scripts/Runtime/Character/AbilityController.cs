using UnityEngine;

public class AbilityController : MonoBehaviour
{
    public AbilityBase DefaultAbility => _defaultAbility;
    
    [Header("Abilities")]
    [SerializeField] private AbilityBase _defaultAbility;
}
