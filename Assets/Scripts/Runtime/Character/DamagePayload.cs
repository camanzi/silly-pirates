public enum DamageType { None, Physical, Fire, Ice, Lightning, Stellar }

public struct DamagePayload
{
    public float Amount;
    public DamageType Type;
    public bool IsCritical;
    public float ResistanceMultiplier; // 1f = neutral; <1 = resisted; >1 = weakness (future use)

    public DamagePayload(float amount, DamageType type = DamageType.None)
    {
        Amount = amount;
        Type = type;
        IsCritical = false;
        ResistanceMultiplier = 1f;
    }
}
