public interface ITurnAgent
{
    public TurnRenderingAgentDataSO RenderingData { get; }
    public TurnAgentDataSO AgentData { get; }
    public TurnAgentEventChannel OnAgentJoin { get; }
    public TurnAgentEventChannel OnAgentLeave { get; }
    public InteractableProximityEventChannel ProximityChannel { get; }

    public void OnCombatJoin();

    public void OnCombatLeave();

    public void OnStartingTurn();
    public bool CompareTag(string tag);
}