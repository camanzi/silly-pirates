using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AbilityController : MonoBehaviour
{
    public AbilityBase defaultAbility => _defaultAbility;
    [SerializeField] private AbilityBase _defaultAbility;

}
