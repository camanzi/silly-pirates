public readonly struct TargetCounterPayload
{
    public readonly bool IsActive;
    public readonly int Current;
    public readonly int Max;

    public TargetCounterPayload(bool isActive, int current, int max)
    {
        IsActive = isActive;
        Current = current;
        Max = max;
    }

    public static TargetCounterPayload Inactive => new(false, 0, 0);
}
