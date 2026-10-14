using System;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// A plain-class IShipTurnParticipant that counts how often the ship told it it had acted. OnTurnEnded runs
    /// inside the call, so a test can make it unregister itself or throw — the two things a real participant
    /// does from inside ShipTurnRosterSO.NotifyShipTurnEnded.
    /// </summary>
    internal class FakeShipTurnParticipant : IShipTurnParticipant
    {
        internal int ShipTurnEndedCalls;
        internal Action OnTurnEnded;

        public void OnShipTurnEnded()
        {
            ShipTurnEndedCalls++;
            OnTurnEnded?.Invoke();
        }
    }

    /// <summary>
    /// A participant that is also an IAgilityModifier: the shape of AgilityBoosterEquipment without the
    /// MonoBehaviour, the state machine and the stats asset. The bonus is stated inline by the test.
    /// </summary>
    internal sealed class FakeAgilityBoosterParticipant : FakeShipTurnParticipant, IAgilityModifier
    {
        private readonly int _flat;
        private readonly float _percentage;

        internal FakeAgilityBoosterParticipant(int flat = 0, float percentage = 0f)
        {
            _flat = flat;
            _percentage = percentage;
        }

        public int GetFlatAgilityBonus() => _flat;

        public float GetPercentageAgilityBonus() => _percentage;
    }
}
