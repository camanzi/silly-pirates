using System;
using UnityEngine;
using UnityEngine.Events;

public class ShootingEquipment : InteractableGridElement, IAwakable
{
    [Header("Awakable configs")]
    // FIXME Later, magari da spostare in un SO??
    [SerializeField] private int _toAwakePoints;

    [Header("Shooting Equipment configs")]
    [Space]
    [SerializeField] private UnityEvent _onShootEffects;
    public UnityEvent OnShootEffects => _onShootEffects;

    public int MaxAwakeningPoints => _toAwakePoints;
    public int CurrentAwakingPoints => _awakingPoints;
    public bool IsAwake => _awakingPoints >= _toAwakePoints;
    public Action OnDataChanged { get => _onDataChanged; set => _onDataChanged = value; }

    private Action _onDataChanged;

    private int _awakingPoints = 0;
    public void AddAwakingPoints(int count)
    {
        _awakingPoints += count;
        _onDataChanged?.Invoke();
    }

    public void RemoveAwakingPoints(int count)
    {
        _awakingPoints -= count;
        if (_awakingPoints <= 0)
        {
            _awakingPoints = 0;
            Debug.LogWarning($"ATTENZIONE, gli awaking points NON possono andare sotto 0");
        }

        _onDataChanged?.Invoke();
    }

    public void ConsumeAllAwakingPoints()
    {
        _awakingPoints = 0;
        _onDataChanged?.Invoke();
    }
}
