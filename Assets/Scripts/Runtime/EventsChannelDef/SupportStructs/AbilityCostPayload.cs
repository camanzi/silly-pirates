public struct AbilityCostPayload
{
    public int ApCost;
    public int MpCost;

    public AbilityCostPayload(int apCost, int mpCost)
    {
        ApCost = apCost;
        MpCost = mpCost;
    }

    public static readonly AbilityCostPayload Empty = new AbilityCostPayload(0, 0);
}
