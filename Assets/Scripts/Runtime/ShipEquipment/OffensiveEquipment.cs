using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ship equipment that deals with enemies. It is never fired by the crew directly: once awakened it arms
/// itself in the <see cref="BroadsideRosterSO"/>, picking its targets with its own
/// <see cref="BroadsideTargetSelectorSO"/>, and fires during the ship's turn together with every other armed
/// equipment (see <see cref="ShipBroadsideController"/>).
/// </summary>
public class OffensiveEquipment : ShipEquipment, IDMGTypeOwner
{
    [Header("Broadside")]
    [Tooltip("Where this equipment arms itself when awakened, to fire during the ship's turn")]
    [SerializeField] private BroadsideRosterSO _broadsideRoster;
    [Tooltip("How this equipment picks its targets, both on awakening and when refilling at firing time")]
    [SerializeField] private BroadsideTargetSelectorSO _targetSelector;

    private readonly List<IDMGTypeModifier> _dmgTypeModifiers = new();
    private readonly List<ITargettable> _candidatesBuffer = new();

    public DamageType EffectiveDMGType =>
        _dmgTypeModifiers.Count > 0
            ? _dmgTypeModifiers[^1].GetDMGTypeOverride()
            : DamageType.None;

    public BroadsideTargetSelectorSO TargetSelector => _targetSelector;

    /// <summary>The ability the broadside fires: the equipment's default one.</summary>
    public AbilityBase BroadsideAbility => AbilityController != null ? AbilityController.DefaultAbility : null;

    public void AddDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Add(m);
    public void RemoveDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Remove(m);

    // The state machine is resolved in ShipEquipment.Awake, which always runs before this OnEnable.
    protected override void OnEnable()
    {
        base.OnEnable();
        if (StateMachine != null) StateMachine.OnActiveChanged += HandleActiveChanged;

        // Re-enabled while still awake: arm again, the roster lost it in OnDisable.
        if (IsAwake) Arm();
    }

    protected override void OnDisable()
    {
        if (StateMachine != null) StateMachine.OnActiveChanged -= HandleActiveChanged;
        if (_broadsideRoster != null) _broadsideRoster.Unregister(this);
        base.OnDisable();
    }

    private void HandleActiveChanged(bool isActive)
    {
        if (isActive) Arm();
        else if (_broadsideRoster != null) _broadsideRoster.Unregister(this);
    }

    private void Arm()
    {
        if (_broadsideRoster == null) return;

        var targets = new List<ITargettable>();
        SelectTargets(targets);
        _broadsideRoster.Register(this, targets);
    }

    /// <summary>
    /// Fills <paramref name="results"/> with fresh targets picked by this equipment's selector among the
    /// current legal candidates. Empty when nothing can be shot or no selector is assigned.
    /// </summary>
    public void SelectTargets(List<ITargettable> results)
    {
        results.Clear();
        AbilityBase ability = BroadsideAbility;
        if (_targetSelector == null || ability == null || _broadsideRoster == null) return;

        BroadsideCandidateQuery.Collect(_broadsideRoster.TurnOrder, this, ability, _candidatesBuffer);
        if (_candidatesBuffer.Count == 0) return;

        var context = new BroadsideTargetingContext(this, ability,
            BroadsideTargetingContext.ResolveTargetCount(ability), _candidatesBuffer,
            _broadsideRoster.TurnOrder, _broadsideRoster.GridState);
        _targetSelector.SelectTargets(context, results);
    }
}
