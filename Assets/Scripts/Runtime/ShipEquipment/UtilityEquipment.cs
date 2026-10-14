/// <summary>
/// Ship equipment that neither deals damage nor heals. What it does, and whether it takes part in the ship's
/// turn (<see cref="IShipTurnParticipant"/>), is up to each subclass.
/// </summary>
public abstract class UtilityEquipment : ShipEquipment { }
