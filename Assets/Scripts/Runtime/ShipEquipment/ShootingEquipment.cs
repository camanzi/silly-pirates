using System;
using UnityEngine;
using UnityEngine.Events;

// FIXME Later, questo dovrá diventare una macchina a stati in piena regola, ma per il momento va bene cosí
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
    public Action OnDataChanged { get => _onDataChanged; set => _onDataChanged = value; }
    protected int AwakingPoints { 
        get => _awakingPoints; 
        set
        {
            _awakingPoints = value;
            if (_awakingPoints >= _toAwakePoints) AwakeningStatus = EAwakeningStatus.Active;         
        }
    }
    public EAwakeningStatus AwakeningStatus 
    { 
        get => _awakeningStatus; 
        set
        {
            _awakeningStatus = value;
            Debug.Log($"Sono entrato in {_awakeningStatus}");
        }
    }
    public int Cooldown { get => _cooldown; set => _cooldown = value; }

    public bool IsAwake => EAwakeningStatus.Active.Equals(_awakeningStatus);

    private Action _onDataChanged;
    private int _cooldown = 0;
    private EAwakeningStatus _awakeningStatus = EAwakeningStatus.Awakable;
    private int _awakingPoints = 0;
    public void AddAwakingPoints(int count)
    {
        AwakingPoints += count;
        _onDataChanged?.Invoke();
    }

    public void RemoveAwakingPoints(int count)
    {
        AwakingPoints -= count;
        if (AwakingPoints < _toAwakePoints)
        {
            AwakeningStatus = EAwakeningStatus.Awakable;
        }

        if (AwakingPoints <= 0)
        {
            AwakingPoints = 0;
            Debug.LogWarning($"ATTENZIONE, gli awaking points NON possono andare sotto 0");
        }

        _onDataChanged?.Invoke();
    }

    public void ConsumeAllAwakingPoints()
    {
        AwakingPoints = 0;
        AwakeningStatus = EAwakeningStatus.Cooldownm;
        _onDataChanged?.Invoke();
    }

    public void OnTurnChange()
    {
        _cooldown--;
        if (_cooldown <= 0) AwakeningStatus = EAwakeningStatus.Awakable;
    }
}
