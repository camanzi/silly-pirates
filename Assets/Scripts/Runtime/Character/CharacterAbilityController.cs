using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterAbilityController : MonoBehaviour
{
    public AbilityBase defaultMoveAbility => _defaultMoveAbility;
    [SerializeField] private AbilityBase _defaultMoveAbility;

    
}
