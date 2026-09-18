namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// A canned IAgilityModifier. The real implementors are ScriptableObjects with private serialized fields
    /// and their own stack rules; this one exists so StatUtils can be tested on the arithmetic alone, with the
    /// contribution stated inline in the test that reads it.
    /// </summary>
    internal sealed class FakeAgilityModifier : IAgilityModifier
    {
        private readonly int _flat;
        private readonly float _percentage;

        internal FakeAgilityModifier(int flat = 0, float percentage = 0f)
        {
            _flat = flat;
            _percentage = percentage;
        }

        public int GetFlatAgilityBonus() => _flat;

        public float GetPercentageAgilityBonus() => _percentage;
    }

    /// <summary>A canned IEvasionModifier, for the same reason as <see cref="FakeAgilityModifier"/>.</summary>
    internal sealed class FakeEvasionModifier : IEvasionModifier
    {
        private readonly int _bonus;

        internal FakeEvasionModifier(int bonus) => _bonus = bonus;

        public int GetEvasionBonus() => _bonus;
    }
}
