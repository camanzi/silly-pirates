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

            // Le parti sono figlie della root, non del pivot animato: senza registrarle resterebbero
            // immobili e opache mentre il corpo emerge, affonda o salta.
            _lifecycleAnimator?.RegisterSatellite(binding.PartHealth.transform);
        }

        if (_lifecycleAnimator == null) return;

        _lifecycleAnimator.OnPhaseStarted += HandleLifecyclePhaseStarted;
        _lifecycleAnimator.OnPhaseCompleted += HandleLifecyclePhaseCompleted;

        // Se il personaggio ha già avviato una fase prima di questo Awake, l'evento di inizio è andato
        // perso: lo si recupera qui.
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
        // Dopo l'uscita dal combattimento le barre restano spente: il nemico sta affondando.
        if (phase == LifecyclePhase.Spawn) SetPartsUIAllowed(true);
    }

    /// <summary>
    /// Le barre vita delle parti seguono il transform della parte: durante emersione e affondamento
    /// scivolerebbero sott'acqua a piena opacità, dato che sono UI Toolkit e non sfumano con lo sprite.
    /// Una parte già rotta non viene riaccesa: la sua barra è stata spenta alla morte da
    /// <see cref="WorldSpaceCharacterStatusController"/> e deve restare tale.
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
