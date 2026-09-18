using UnityEngine;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// Builds a TurnAgentDataSO in memory. Needed because every field of that SO is private +
    /// [SerializeField]; the two properties this builder writes carry an internal setter exposed to the
    /// test assemblies through InternalsVisibleTo (see Assets/Scripts/Runtime/AssemblyInfo.cs).
    ///
    /// The caller owns the returned instance and must destroy it — pass it through
    /// ScriptableObjectFixture.Track so TearDown does it.
    /// </summary>
    internal sealed class TurnAgentDataBuilder
    {
        private int _maxActionPointsPerTurn = 5;
        private bool _randomizeInitialAV;

        internal static TurnAgentDataBuilder New() => new TurnAgentDataBuilder();

        internal TurnAgentDataBuilder WithMaxActionPoints(int value)
        {
            _maxActionPointsPerTurn = value;
            return this;
        }

        internal TurnAgentDataBuilder WithRandomizeInitialAV(bool value)
        {
            _randomizeInitialAV = value;
            return this;
        }

        internal TurnAgentDataSO Build()
        {
            TurnAgentDataSO data = ScriptableObject.CreateInstance<TurnAgentDataSO>();
            data.MaxActionPointsPerTurn = _maxActionPointsPerTurn;
            data.RandomizeInitialAV = _randomizeInitialAV;
            return data;
        }
    }
}
