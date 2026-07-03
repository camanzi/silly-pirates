public readonly struct TurnGroupDisplay
{
    public readonly EntityTurnState State;
    public readonly int SubTurnCount;

    public TurnGroupDisplay(EntityTurnState state, int subTurnCount)
    {
        State = state;
        SubTurnCount = subTurnCount;
    }
}
