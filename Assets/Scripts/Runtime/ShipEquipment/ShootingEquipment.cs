using UnityEngine;
using UnityEngine.Events;

public class ShootingEquipment : InteractableGridElement
{
    [Space]
    [SerializeField] private UnityEvent _onShootEffects;
    public UnityEvent OnShootEffects => _onShootEffects;
}
