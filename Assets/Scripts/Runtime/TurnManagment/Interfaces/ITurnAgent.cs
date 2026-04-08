public interface ITurnAgent
{
    public TurnAgentDataSO AgentData { get; }
    public TurnAgentEventChannel OnAgentJoin { get; }
    public TurnAgentEventChannel OnAgentLeave { get; }
    
    public void OnCombatJoin();

    public void OnCombatLeave();

    public void OnStartingTurn();
}