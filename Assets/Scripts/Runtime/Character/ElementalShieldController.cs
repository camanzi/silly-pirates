using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The heart of the elemental shield. It lives on the part (PF_ElementalShield) and rewrites at runtime
/// the resistances of the character wearing it.
///
/// Shield up: the owner is resistant to all four elements except the selected one, which instead HEALS it
/// for the damage dealt — the drawn element is the one to avoid on the body, but it also remains the only
/// one that breaks the shield part (see <see cref="ElementalShieldHitBehaviorSO"/>), so it has to be
/// unloaded there and never on the body. Shield broken: the owner loses everything and becomes vulnerable
/// to all four, until the part's PartRegenBehaviorSO puts it back up.
///
/// Every behavior granted to the owner is tracked in <see cref="_granted"/>: revocation touches only those,
/// never the character's own _baseBehaviors, which stay active and compose with these.
/// </summary>
[RequireComponent(typeof(HealthController))]
public class ElementalShieldController : MonoBehaviour
{
    [Serializable]
    private struct ElementBehaviors
    {
        public DamageType Element;
        public ResistanceBehaviorSO Resistance;
        public AbsorptionBehaviorSO Absorption;
        public VulnerabilityBehaviorSO Vulnerability;
        public Color Tint;
    }

    [Header("Elements")]
    [Tooltip("One row per element (Physical, Fire, Ice, Lightning): the resistance, absorption and vulnerability assets to grant to the owner.")]
    [SerializeField] private ElementBehaviors[] _elements;

    [Header("Shield configuration")]
    [Tooltip("Hits with the active element needed to break the shield. Each hit is worth MaxHp / this value.")]
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

        // The part's HealthController is what gets listened to, not IPartOwner.OnPartBroken: the latter has
        // no "repaired" twin and re-fires on every regeneration, whereas both transitions are needed here.
        _shieldHealth.OnDeath  += HandleBreak;
        _shieldHealth.OnRevive += HandleRestore;
    }

    private void Start() => SetElement(PickRandomElement(), silent: true);

    private void OnDestroy()
    {
        if (_shieldHealth == null) return;
        _shieldHealth.OnDeath  -= HandleBreak;
        _shieldHealth.OnRevive -= HandleRestore;
    }

    /// <summary>Counts one hit per element. Called by both the shield and the owner, including for hits that absorption has turned into healing.</summary>
    public void RegisterHit(DamageType type)
    {
        if (type == DamageType.None) return;
        _hitCounts.TryGetValue(type, out int count);
        _hitCounts[type] = count + 1;
    }

    /// <summary>
    /// Of the three elements other than the active one, the one the player has used the most: switching to
    /// it turns the cannon they already have in hand into the tool that heals the enemy instead of hurting
    /// it, denying their plan. Ties are broken at random.
    /// </summary>
    public DamageType PickDenialElement()
    {
        var candidates = new List<DamageType>();
        int best = int.MinValue;

        for (int i = 0; i < _elements.Length; i++)
        {
            DamageType element = _elements[i].Element;
            if (element == DamageType.None || element == ActiveElement) continue;

            _hitCounts.TryGetValue(element, out int count);
            if (count > best)
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

    /// <summary>Makes the owner resistant to all four elements except <paramref name="element"/>, which heals it instead.</summary>
    public void SetElement(DamageType element, bool silent = false)
    {
        RevokeAll();

        for (int i = 0; i < _elements.Length; i++)
        {
            var row = _elements[i];
            if (row.Element == DamageType.None) continue;
            Grant(row.Element == element ? (HealthBehaviorSO)row.Absorption : row.Resistance);
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

        // The tracker stays even with the shield broken: the hits taken now still tell what the player is holding.
        GrantTracker();

        if (_breakPassive != null && _owner != null && _owner.PassiveAbilityController != null)
            _owner.PassiveAbilityController.AddPassive(Instantiate(_breakPassive));
    }

    // Regeneration redraws the element from all four, including the one just used to break the shield:
    // under the new rule drawing it again is no gift, it is the best trap there is, because the cannon the
    // player has just used to punch through the part would go back to healing the body.
    private void HandleRestore() => SetElement(PickRandomElement());

    private DamageType PickRandomElement()
    {
        var candidates = new List<DamageType>();
        for (int i = 0; i < _elements.Length; i++)
        {
            DamageType element = _elements[i].Element;
            if (element != DamageType.None) candidates.Add(element);
        }

        if (candidates.Count == 0) return DamageType.None;
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

    /// <summary>
    /// The colour associated with an element. Exposed because the guard feedback
    /// (<see cref="ElementalShieldGuardAnimator"/>) has to flash with the colour of the INCOMING element,
    /// which is not the active one and therefore cannot be read off the sprite's current tint.
    /// </summary>
    public bool TryGetTint(DamageType element, out Color tint)
    {
        for (int i = 0; i < _elements.Length; i++)
            if (_elements[i].Element == element)
            {
                tint = _elements[i].Tint;
                return true;
            }

        tint = Color.white;
        return false;
    }

    private void ApplyTint(DamageType element)
    {
        if (_spriteRenderer == null) return;
        if (TryGetTint(element, out Color tint)) _spriteRenderer.color = tint;
    }

    private void RaiseElementChangeVfx()
    {
        if (_elementChangeVfx == null || _vfxChannel == null) return;
        _vfxChannel.RaiseEvent(VfxCue.At(_elementChangeVfx, transform.position));
    }
}
