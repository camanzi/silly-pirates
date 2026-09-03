using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuore dello scudo elementale. Vive sulla parte (PF_ElementalShield) e riscrive a runtime le resistenze
/// del personaggio che la indossa.
///
/// Scudo attivo: l'owner e' resistente all'elemento selezionato e immune agli altri tre, quindi l'unico
/// modo di fargli danno e' indovinare l'elemento corrente. Scudo rotto: l'owner perde tutto e diventa
/// vulnerabile a tutti e quattro, finche' il PartRegenBehaviorSO della parte non lo rimette in piedi.
///
/// Tutti i behavior concessi all'owner sono tracciati in <see cref="_granted"/>: la revoca tocca solo
/// quelli, mai i _baseBehaviors del personaggio, che restano attivi e si compongono con questi.
/// </summary>
[RequireComponent(typeof(HealthController))]
public class ElementalShieldController : MonoBehaviour
{
    [Serializable]
    private struct ElementBehaviors
    {
        public DamageType Element;
        public ResistanceBehaviorSO Resistance;
        public ImmunityBehaviorSO Immunity;
        public VulnerabilityBehaviorSO Vulnerability;
        public Color Tint;
    }

    [Header("Elementi")]
    [Tooltip("Una riga per elemento (Physical, Fire, Ice, Lightning): gli asset di resistenza, immunita' e vulnerabilita' da concedere all'owner.")]
    [SerializeField] private ElementBehaviors[] _elements;

    [Header("Configurazione scudo")]
    [Tooltip("Colpi con l'elemento attivo necessari a rompere lo scudo. Ogni colpo vale MaxHp / questo valore.")]
    [SerializeField] private int _hitsToBreak = 3;
    [SerializeField] private ElementalShieldTrackerBehaviorSO _tracker;
    [SerializeField] private ShieldBreakSlowPassiveSO _breakPassive;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private VFXController _elementChangeVfx;
    [SerializeField] private VfxCueEventChannel _vfxChannel;

    private HealthController _shieldHealth;
    private HostileCharacter _owner;
    private readonly List<HealthBehaviorSO> _granted = new();
    private readonly Dictionary<DamageType, int> _hitCounts = new();

    public DamageType ActiveElement { get; private set; } = DamageType.None;
    public int HitsToBreak => _hitsToBreak;
    public bool IsActive => _shieldHealth != null && _shieldHealth.IsAlive;

    private void Awake()
    {
        _shieldHealth = GetComponent<HealthController>();
        _owner = GetComponentInParent<HostileCharacter>();

        // Si ascolta l'HealthController della parte e non IPartOwner.OnPartBroken: quest'ultimo non ha un
        // gemello "riparato" e riemette a ogni rigenerazione, mentre qui servono entrambe le transizioni.
        _shieldHealth.OnDeath  += HandleBreak;
        _shieldHealth.OnRevive += HandleRestore;
    }

    private void Start() => SetElement(PickRandomElement(DamageType.None), silent: true);

    private void OnDestroy()
    {
        if (_shieldHealth == null) return;
        _shieldHealth.OnDeath  -= HandleBreak;
        _shieldHealth.OnRevive -= HandleRestore;
    }

    /// <summary>Conta un colpo per elemento. Chiamato sia dallo scudo sia dall'owner, anche per i colpi azzerati dall'immunita'.</summary>
    public void RegisterHit(DamageType type)
    {
        if (type == DamageType.None) return;
        _hitCounts.TryGetValue(type, out int count);
        _hitCounts[type] = count + 1;
    }

    /// <summary>
    /// Fra i tre elementi diversi da quello attivo, quello che il giocatore ha usato meno: passare li'
    /// rende inutili le cannoniere che ha in mano e lo costringe a ri-adattarsi. Parita' risolta a caso.
    /// </summary>
    public DamageType PickDenialElement()
    {
        var candidates = new List<DamageType>();
        int best = int.MaxValue;

        for (int i = 0; i < _elements.Length; i++)
        {
            DamageType element = _elements[i].Element;
            if (element == DamageType.None || element == ActiveElement) continue;

            _hitCounts.TryGetValue(element, out int count);
            if (count < best)
            {
                best = count;
                candidates.Clear();
                candidates.Add(element);
            }
            else if (count == best)
            {
                candidates.Add(element);
            }
        }

        return candidates.Count == 0 ? ActiveElement : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>Rende l'owner resistente a <paramref name="element"/> e immune agli altri tre.</summary>
    public void SetElement(DamageType element, bool silent = false)
    {
        RevokeAll();

        for (int i = 0; i < _elements.Length; i++)
        {
            var row = _elements[i];
            if (row.Element == DamageType.None) continue;
            Grant(row.Element == element ? (HealthBehaviorSO)row.Resistance : row.Immunity);
        }

        GrantTracker();

        ActiveElement = element;
        ApplyTint(element);

        if (!silent) RaiseElementChangeVfx();
    }

    private void HandleBreak()
    {
        RevokeAll();

        for (int i = 0; i < _elements.Length; i++)
            if (_elements[i].Element != DamageType.None) Grant(_elements[i].Vulnerability);

        // Il tracker resta anche a scudo rotto: i colpi presi ora dicono comunque cosa ha in mano il giocatore.
        GrantTracker();

        if (_breakPassive != null && _owner != null && _owner.PassiveAbilityController != null)
            _owner.PassiveAbilityController.AddPassive(Instantiate(_breakPassive));
    }

    // La rigenerazione riestrae l'elemento: ripartire da quello appena bucato regalerebbe un turno gratis.
    private void HandleRestore() => SetElement(PickRandomElement(ActiveElement));

    private DamageType PickRandomElement(DamageType exclude)
    {
        var candidates = new List<DamageType>();
        for (int i = 0; i < _elements.Length; i++)
        {
            DamageType element = _elements[i].Element;
            if (element != DamageType.None && element != exclude) candidates.Add(element);
        }

        if (candidates.Count == 0) return exclude;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void Grant(HealthBehaviorSO template)
    {
        if (template == null || _owner == null || _owner.Health == null) return;

        var clone = Instantiate(template);
        _granted.Add(clone);
        _owner.Health.AddBehavior(clone);
    }

    private void GrantTracker()
    {
        if (_tracker == null || _owner == null || _owner.Health == null) return;

        var clone = Instantiate(_tracker);
        clone.Bind(this);
        _granted.Add(clone);
        _owner.Health.AddBehavior(clone);
    }

    private void RevokeAll()
    {
        if (_owner != null && _owner.Health != null)
            for (int i = 0; i < _granted.Count; i++)
                _owner.Health.RemoveBehavior(_granted[i]);

        _granted.Clear();
    }

    private void ApplyTint(DamageType element)
    {
        if (_spriteRenderer == null) return;

        for (int i = 0; i < _elements.Length; i++)
            if (_elements[i].Element == element)
            {
                _spriteRenderer.color = _elements[i].Tint;
                return;
            }
    }

    private void RaiseElementChangeVfx()
    {
        if (_elementChangeVfx == null || _vfxChannel == null) return;
        _vfxChannel.RaiseEvent(VfxCue.At(_elementChangeVfx, transform.position));
    }
}
