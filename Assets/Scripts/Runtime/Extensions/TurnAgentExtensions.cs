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
        element.ProximityChannel.RaiseEvent(payload);
    }
}