/// <summary>
/// The single place that decides what kind of turn agent something is, and whether the player is in control
/// while it holds the turn. Every consumer used to call CompareTag("Player") on its own: route through here
/// instead, so that adding a new kind of agent (the ship) means changing one file, not a dozen.
///
/// There is still no team enum in the project: an enemy is a HostileCharacter (see TurnOrderQueries), and
/// these tags only tell the player's side apart.
/// </summary>
public static class TurnAgentRoles
{
    public const string CrewTag = "Player";
    public const string ShipTag = "Ship";

    /// <summary>A member of the player's crew. Drives the crew HUD and the "once per crew turn" ticks.</summary>
    public static bool IsCrewMember(ITurnAgent agent) => agent != null && agent.CompareTag(CrewTag);

    /// <summary>The player's ship, which has a turn of its own in the queue.</summary>
    public static bool IsShip(ITurnAgent agent) => agent != null && agent.CompareTag(ShipTag);

    /// <summary>
    /// Whether the player has UI and input agency while this agent holds the turn (End Turn button, AP
    /// display, camera pan, ability selection).
    ///
    /// Today this is the same as <see cref="IsCrewMember"/>: the ship's turn is automatic and is treated like
    /// an enemy's. Kept as its own entry point on purpose: the day the ship gives the player agency, only this
    /// line changes.
    /// </summary>
    public static bool HasPlayerAgency(ITurnAgent agent) => IsCrewMember(agent);
}
