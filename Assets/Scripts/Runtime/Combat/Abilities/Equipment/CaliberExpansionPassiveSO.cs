using UnityEngine;

[CreateAssetMenu(fileName = "Caliber Expansion Passive", menuName = "Abilities/Equipment/Caliber Expansion Passive")]
public class CaliberExpansionPassiveSO : PassiveAbilitySO, ICritDMGModifier
{
    [Header("Caliber Expansion Passive Configs")]
    [SerializeField] private int _critDMGBonus = 10;

    private PassiveAbilityController _controller;
    private ITurnAgent _caster;
    private TurnAgentEventChannel _onAnyTurnEnded;

    public void Initialize(ITurnAgent caster, TurnAgentEventChannel onAnyTurnEnded)
    {
        _caster = caster;
        _onAnyTurnEnded = onAnyTurnEnded;
    }

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.AddCritDMGModifier(this);
        _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.RemoveCritDMGModifier(this);
        _controller = null;
    }

    int ICritDMGModifier.GetCritDMGBonus() => _critDMGBonus;

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _controller.RemovePassive(this);
    }
}
