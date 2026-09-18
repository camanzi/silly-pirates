namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// A plain-class ITurnAgent. Deliberately not a MonoBehaviour: TurnOrderDataSO.AddEntity logs only for
    /// MonoBehaviour agents, so a plain fake keeps the console clean, and there is no GameObject to destroy.
    ///
    /// Everything the turn system reads is settable, EffectiveAgility included, so a test can change an
    /// agent's agility after it joined the queue and then assert what UpdateAgentAV does with it.
    /// </summary>
    internal sealed class FakeTurnAgent : ITurnAgent
    {
        internal string Tag = "Untagged";

        public TurnRenderingAgentDataSO RenderingData { get; set; }
        public TurnAgentDataSO AgentData { get; set; }
        public TurnAgentEventChannel OnAgentJoin { get; set; }
        public TurnAgentEventChannel OnAgentLeave { get; set; }
        public IntEventChannel OnAPChanged { get; set; }
        public InteractableProximityEventChannel ProximityChannel { get; set; }
        public int RemainingActionPoints { get; set; }
        public int EffectiveAgility { get; set; } = 100;
        public string DisplayName { get; set; } = "Fake";

        // HealthController is a MonoBehaviour and nothing under test in this tranche reads it.
        public HealthController Health => null;

        internal int CombatJoinCalls;
        internal int CombatLeaveCalls;
        internal int StartingTurnCalls;
        internal int ContinuingTurnCalls;
        internal int EndingTurnCalls;

        public void OnCombatJoin() => CombatJoinCalls++;
        public void OnCombatLeave() => CombatLeaveCalls++;
        public void OnStartingTurn() => StartingTurnCalls++;
        public void OnContinuingTurn() => ContinuingTurnCalls++;
        public void OnEndingTurn() => EndingTurnCalls++;

        public bool CompareTag(string tag) => Tag == tag;
    }
}
