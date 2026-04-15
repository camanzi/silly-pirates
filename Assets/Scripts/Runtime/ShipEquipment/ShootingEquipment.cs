using System;
using UnityEngine;
using UnityEngine.Events;

public class ShootingEquipment : InteractableGridElement, IAwakable
{
    [Header("Awakable configs")]
    [SerializeField] private int _maxAwakePoints;

    [Header("Shooting Equipment configs")]
    [Space]
    [SerializeField] private UnityEvent _onShootEffects;
    public UnityEvent OnShootEffects => _onShootEffects;

    private int _awakingPoints = 0;
    public int AwakingPoints => _awakingPoints;

    public void AddAwakingPoints(int count) => _awakingPoints += count;

    public bool IsAwake() => _awakingPoints >= _maxAwakePoints;

    public void RemoveAwakingPoints(int count)
    {
        _awakingPoints += count;
        if (_awakingPoints <= 0)
        {
            _awakingPoints = 0;
            Debug.LogWarning($"ATTENZIONE, gli awaking points NON possono andare sotto 0");
        }
    }
}
