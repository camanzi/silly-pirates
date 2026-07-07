using UnityEngine;

public abstract class PassiveAbilitySO : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private PassiveRemovalTiming _removalTiming = PassiveRemovalTiming.AnyTurn;

    [Header("UI Rendering")]
    [SerializeField] private Sprite _icon;
    [SerializeField, TextArea(2, 5)] private string _description;

    public virtual string DisplayName => _displayName;
    public PassiveRemovalTiming RemovalTiming => _removalTiming;
    public virtual Sprite Icon => _icon;
    public virtual string Description => _description;

    public abstract void OnEquip(PassiveAbilityController controller);
    public abstract void OnUnequip(PassiveAbilityController controller);

    // Default: true (always shown once equipped). Passives with transient on/off
    // state (e.g. stack-based buffs) override this to control whether their icon
    // is present in the HUD list at all — there is no dimmed "inactive" visual;
    // the icon simply is not rendered in the crew HUD when this returns false.
    public virtual bool IsCurrentlyActive(PassiveAbilityController controller) => true;
}
