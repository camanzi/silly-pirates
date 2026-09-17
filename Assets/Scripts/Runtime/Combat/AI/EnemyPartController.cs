using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPartController : MonoBehaviour
{
    [SerializeField] private List<PartBinding> _parts;

    public event Action<EnemyPartSO> OnPartBroken;

    private CharacterLifecycleAnimator _lifecycleAnimator;
    private WorldSpaceContainer[] _partUis;

    private void Awake()
    {
        _lifecycleAnimator = GetComponent<CharacterLifecycleAnimator>();
        _partUis = new WorldSpaceContainer[_parts.Count];

        for (int i = 0; i < _parts.Count; i++)
        {
            var binding = _parts[i];
            if (binding.PartHealth == null) continue;

            var part = binding.Part;
            binding.PartHealth.OnDeath += () => OnPartBroken?.Invoke(part);

            _partUis[i] = binding.PartHealth.GetComponentInChildren<WorldSpaceContainer>(true);

            // The parts are children of the root, not of the animated pivot: without registering them
            // they would stay motionless and opaque while the body emerges, sinks or jumps.
            _lifecycleAnimator?.RegisterSatellite(binding.PartHealth.transform);
        }

        if (_lifecycleAnimator == null) return;

        _lifecycleAnimator.OnPhaseStarted += HandleLifecyclePhaseStarted;
        _lifecycleAnimator.OnPhaseCompleted += HandleLifecyclePhaseCompleted;

        // If the character has already started a phase before this Awake, the start event was lost:
        // it is caught up with here.
        if (_lifecycleAnimator.ActivePhase.HasValue) SetPartsUIAllowed(false);
    }

    private void OnDestroy()
    {
        if (_lifecycleAnimator == null) return;

        _lifecycleAnimator.OnPhaseStarted -= HandleLifecyclePhaseStarted;
        _lifecycleAnimator.OnPhaseCompleted -= HandleLifecyclePhaseCompleted;
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

    private void HandleLifecyclePhaseStarted(LifecyclePhase phase) => SetPartsUIAllowed(false);

    private void HandleLifecyclePhaseCompleted(LifecyclePhase phase)
    {
        // After leaving combat the bars stay off: the enemy is sinking.
        if (phase == LifecyclePhase.Spawn) SetPartsUIAllowed(true);
    }

    /// <summary>
    /// The parts' health bars follow the part's transform: while emerging and sinking they would slide
    /// underwater at full opacity, since they are UI Toolkit and do not fade along with the sprite.
    /// An already broken part is never turned back on: its bar was switched off on death by
    /// <see cref="WorldSpaceCharacterStatusController"/> and must stay that way.
    /// </summary>
    private void SetPartsUIAllowed(bool allowed)
    {
        if (_partUis == null) return;

        for (int i = 0; i < _partUis.Length; i++)
        {
            if (_partUis[i] == null) continue;

            var health = _parts[i].PartHealth;
            if (allowed && (health == null || !health.IsAlive)) continue;

            _partUis[i].SetElementStatePermission(allowed);
        }
    }
}

[Serializable]
public struct PartBinding
{
    public EnemyPartSO Part;
    public HealthController PartHealth;
}
