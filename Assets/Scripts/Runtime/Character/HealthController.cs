using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour, IDamageable
{
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private float _standaloneMaxHp;
    [SerializeField] private List<HealthBehaviorSO> _baseBehaviors;
    [SerializeField] private DamageEventChannel _showDamageUIEventChannel;

    private List<HealthBehaviorSO> _behaviors = new();
    private float _currentHp;

    public float CurrentHp => _currentHp;
    public float MaxHp => _agentData != null ? _agentData.MaxHp : _standaloneMaxHp;
    public bool IsAlive => _currentHp > 0f;

    public event Action<float> OnHpChanged;
    public event Action OnDeath;
    public event Action OnTakeDamage;
    public event Action OnHealReceived;

    private void Awake()
    {
        if (_agentData == null)
            _agentData = GetComponent<ITurnAgent>()?.AgentData;

        for (int i = 0; i < _baseBehaviors.Count; i++) _behaviors.Add(Instantiate(_baseBehaviors[i]));
        _currentHp = MaxHp;
    }

    private void OnEnable()
    {
        for (int i = 0; i < _behaviors.Count; i++) _behaviors[i].OnEquip(this);
    }

    private void OnDisable()
    {
        for (int i = 0; i < _behaviors.Count; i++) _behaviors[i].OnUnequip(this);
    }

    public void TakeDamage(DamagePayload payload)
    {
        if (!IsAlive) return;
        for (int i = 0; i < _behaviors.Count; i++) payload = _behaviors[i].ModifyIncomingDamage(payload);
        ApplyDamage(payload);
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        for (int i = 0; i < _behaviors.Count; i++) amount = _behaviors[i].ModifyHeal(amount);
        ApplyHeal(amount);
    }

    private void ApplyDamage(DamagePayload payload)
    {
        if (payload.Amount < 0f) { ApplyHeal(-payload.Amount); return; }
        _currentHp = Mathf.Max(0f, _currentHp - payload.Amount);
        OnHpChanged?.Invoke(_currentHp);
        bool died = _currentHp <= 0f;
        for (int i = 0; i < _behaviors.Count; i++)
        {
            _behaviors[i].OnDamageTaken(this, payload);
            if (died) _behaviors[i].OnDeath(this);
        }
        _showDamageUIEventChannel?.RaiseEvent(new DamageEvent { Payload = payload, WorldPosition = transform.position });

        if (died)
        {
            OnDeath?.Invoke();
        } else
        {
            OnTakeDamage?.Invoke();
        }
    }

    private void ApplyHeal(float amount)
    {
        if (amount < 0f) { ApplyDamage(new DamagePayload(-amount)); return; }
        _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
        OnHpChanged?.Invoke(_currentHp);
        for (int i = 0; i < _behaviors.Count; i++) _behaviors[i].OnHealReceived(this, amount);
        OnHealReceived?.Invoke();
    }

    public void OnTurnStart()
    {
        for (int i = 0; i < _behaviors.Count; i++) _behaviors[i].OnTurnStart(this);
    }
}
