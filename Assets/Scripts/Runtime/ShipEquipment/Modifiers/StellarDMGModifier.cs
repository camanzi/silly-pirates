public class StellarDMGModifier : IDMGTypeModifier
{
    private readonly OffensiveEquipment _equipment;
    private readonly TurnAgentEventChannel _channel;
    private readonly ITurnAgent _owner;
    private bool _active = true;

    public StellarDMGModifier(OffensiveEquipment equipment, TurnAgentEventChannel channel, ITurnAgent owner)
    {
        _equipment = equipment;
        _channel = channel;
        _owner = owner;
        _channel.OnEventRaised += OnTurnEnded;
    }

    public DamageType GetDMGTypeOverride() => DamageType.Stellar;

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _owner || !_active) return;
        _active = false;
        _equipment.RemoveDMGTypeModifier(this);
        _channel.OnEventRaised -= OnTurnEnded;
    }
}
