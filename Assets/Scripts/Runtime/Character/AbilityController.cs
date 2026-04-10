using UnityEngine;

public class AbilityController : MonoBehaviour
{
    public AbilityBase defaultAbility => _defaultAbility;
    [SerializeField] private AbilityBase _defaultAbility;

}
