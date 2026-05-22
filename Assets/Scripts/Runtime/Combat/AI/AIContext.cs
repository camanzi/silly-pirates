public struct AIContext
{
    public HostileCharacter Caster;
    public TurnOrderDataSO TurnOrder;
    public GridStateDataSO GridState;

    public AIContext(HostileCharacter caster, TurnOrderDataSO turnOrder, GridStateDataSO gridState)
    {
        Caster = caster;
        TurnOrder = turnOrder;
        GridState = gridState;
    }
}
