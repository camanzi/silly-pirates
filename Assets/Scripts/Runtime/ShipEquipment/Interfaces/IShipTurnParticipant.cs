/// <summary>
/// Ship equipment whose effect is tied to the ship's turn. While awake it sits in the
/// <see cref="ShipTurnRosterSO"/> (<see cref="ShipEquipment"/> registers and unregisters it on its own), and
/// it is told when the ship has acted.
///
/// Independent of <see cref="EquipmentType"/>: not every utility equipment takes part in the ship's turn, and
/// nothing stops another type from doing so. The Broadside keeps its own roster (<see cref="BroadsideRosterSO"/>).
/// </summary>
public interface IShipTurnParticipant
{
    /// <summary>
    /// Called by <see cref="ShipTurnAgent.OnEndingTurn"/>, before the turn order computes the ship's next AV.
    /// </summary>
    void OnShipTurnEnded();
}
