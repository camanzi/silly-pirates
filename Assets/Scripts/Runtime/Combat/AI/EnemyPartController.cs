using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPartController : MonoBehaviour
{
    [SerializeField] private List<PartBinding> _parts;

    public event Action<EnemyPartSO> OnPartBroken;

    private void Awake()
    {
        foreach (var binding in _parts)
        {
            if (binding.PartHealth == null) continue;
            var part = binding.Part;
            binding.PartHealth.OnDeath += () => OnPartBroken?.Invoke(part);
        }
    }

    public bool IsPartFunctional(EnemyPartSO part)
    {
        for (int i = 0; i < _parts.Count; i++)
            if (_parts[i].Part == part)
                return _parts[i].PartHealth != null && _parts[i].PartHealth.IsAlive;
        return true;
    }

    public Transform GetPartTransform(EnemyPartSO part)
    {
        for (int i = 0; i < _parts.Count; i++)
            if (_parts[i].Part == part && _parts[i].PartHealth != null)
                return _parts[i].PartHealth.transform;
        return null;
    }

    public void OnTurnStart()
    {
        for (int i = 0; i < _parts.Count; i++)
            _parts[i].PartHealth?.OnTurnStart();
    }
}

[Serializable]
public struct PartBinding
{
    public EnemyPartSO Part;
    public HealthController PartHealth;
}
