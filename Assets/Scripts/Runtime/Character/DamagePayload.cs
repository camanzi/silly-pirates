public enum DamageType { None, Physical, Fire, Ice, Lightning, Stellar }

public struct DamagePayload
{
    public float Amount;
    public DamageType Type;

    public DamagePayload(float amount, DamageType type = DamageType.None)
    {
        Amount = amount;
        Type = type;
    }
}
