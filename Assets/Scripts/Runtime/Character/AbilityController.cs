using UnityEngine;

public class AbilityController : MonoBehaviour
{
    public AbilityBase DefaultAbility => _defaultAbility;
    public AbilityBase ActiveAbility => _activeAbility;

    [Header("Abilities")]
    [SerializeField] private AbilityBase _defaultAbility;
    [SerializeField] private AbilityBase _activeAbility;
}
