public static class TurnAgentExtensions 
{
    public static void HandleCombatJoin(this ITurnAgent element)
    {
        element.OnAgentJoin?.RaiseEvent(element);
    }

    public static void HandleCombatLeave(this ITurnAgent element)
    {
        element.OnAgentLeave?.RaiseEvent(element);
    }

    public static void HandleStartingTurn(this ITurnAgent element)
    {
        element.RemainingActionPoints = element.AgentData.MaxActionPointsPerTurn;   
    }

    public static void EmitProximityCheck(this ITurnAgent element, ProximityPayload payload)
    {
        // Null-conditional like the three raises above: an agent with no proximity channel assigned is a
        // wiring gap, not a reason to throw in the middle of a turn.
        element.ProximityChannel?.RaiseEvent(payload);
    }
}