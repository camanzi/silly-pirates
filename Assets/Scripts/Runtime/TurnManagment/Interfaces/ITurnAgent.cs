public interface ITurnAgent
{
    public TurnRenderingAgentDataSO RenderingData { get; }
    public TurnAgentDataSO AgentData { get; }
    public TurnAgentEventChannel OnAgentJoin { get; }
    public TurnAgentEventChannel OnAgentLeave { get; }
    public IntEventChannel OnAPChanged { get; }
    public InteractableProximityEventChannel ProximityChannel { get; }
    public int RemainingActionPoints { get; set; }
    public int EffectiveAgility { get; }
    public HealthController Health { get; }
    public string DisplayName { get; }
    public void OnCombatJoin();
    public void OnCombatLeave();
    public void OnStartingTurn();
    public void OnContinuingTurn();
    public bool CompareTag(string tag);
}