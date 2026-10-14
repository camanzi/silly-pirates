/// <summary>
/// Ship equipment that the crew never fires: once awakened it acts by itself (the Broadside for
/// <see cref="OffensiveEquipment"/>, a passive effect on the ship for <see cref="AgilityBoosterEquipment"/>).
/// The crew's only interaction with it is awakening it, so <see cref="ActivateAbilityActionSO"/> refuses it.
/// </summary>
public interface IAutoActivatedEquipment { }
