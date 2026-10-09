using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Extensions
{
    /// <summary>
    /// The per-team alive counters that drive the combat outcome. The rule under test comes from the source
    /// and from ShipTurnAgent's contract: an agent counts only if it is an IHealthOwner with a non-null,
    /// alive HealthController. The ship has no health and must never count, on either side.
    ///
    /// Note: ITurnAgent exposes Health but is not an IHealthOwner, so a plain FakeTurnAgent (and the real
    /// ShipTurnAgent) are rejected by the type check before Health is ever read. The "Health == null"
    /// branch is only reached by an agent that IS an IHealthOwner — hence NullHealthOwnerAgent below.
    ///
    /// Private serialized fields are written through SerializedObject because production code has no
    /// seam for them and this task forbids adding one. FindProperty returns null on a rename, which the
    /// helper turns into a loud failure instead of a silent pass.
    /// </summary>
    public class TurnOrderQueriesTests : ScriptableObjectFixture
    {
        private TurnOrderDataSO _turnOrder;

        [SetUp]
        public void SetUp() => _turnOrder = NewSO<TurnOrderDataSO>();

        // ---------------------------------------------------------------- helpers

        /// <summary>A player-side agent that is an IHealthOwner, with whatever HealthController it is given.</summary>
        private sealed class HealthOwnerAgent : ITurnAgent, IHealthOwner
        {
            public HealthOwnerAgent(HealthController health) => Health = health;

            public HealthController Health { get; }
            public TurnRenderingAgentDataSO RenderingData => null;
            public TurnAgentDataSO AgentData => null;
            public TurnAgentEventChannel OnAgentJoin => null;
            public TurnAgentEventChannel OnAgentLeave => null;
            public IntEventChannel OnAPChanged => null;
            public InteractableProximityEventChannel ProximityChannel => null;
            public int RemainingActionPoints { get; set; }
            public int EffectiveAgility => 100;
            public string DisplayName => "HealthOwner";
            public void OnCombatJoin() { }
            public void OnCombatLeave() { }
            public void OnStartingTurn() { }
            public void OnContinuingTurn() { }
            public void OnEndingTurn() { }
            public bool CompareTag(string tag) => tag == TurnAgentRoles.CrewTag;
        }

        private static void SetSerialized(Object target, string fieldName, System.Action<SerializedProperty> write)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            Assert.That(prop, Is.Not.Null, $"{target.GetType().Name}.{fieldName} not found: was it renamed?");
            write(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A HealthController with hp > 0, built without relying on Awake: standalone max HP, then Revive.</summary>
        private HealthController NewAliveHealth()
        {
            HealthController health = NewInactiveGameObject("alive health").AddComponent<HealthController>();
            SetSerialized(health, "_standaloneMaxHp", p => p.floatValue = 10f);
            health.Revive(10f);
            Assert.That(health.IsAlive, Is.True, "Precondition: the control agent must be alive.");
            return health;
        }

        /// <summary>A HostileCharacter whose Awake never ran, so its cached HealthController is null.</summary>
        private HostileCharacter NewHostileWithNullHealth()
        {
            HostileCharacter hostile = NewInactiveGameObject("hostile").AddComponent<HostileCharacter>();
            TurnAgentDataSO data = Track(TurnAgentDataBuilder.New().Build());
            SetSerialized(hostile, "_agentData", p => p.objectReferenceValue = data); // AddEntity reads EffectiveAgility
            Assert.That(hostile.Health, Is.Null, "Precondition: the hostile must expose a null Health.");
            return hostile;
        }

        private ShipTurnAgent NewShip() => NewInactiveGameObject("ship").AddComponent<ShipTurnAgent>();

        // ---------------------------------------------------------------- CountAlivePlayers

        [Test]
        public void CountAlivePlayers_NullTurnOrder_ReturnsZero()
        {
            Assert.That(TurnOrderQueries.CountAlivePlayers(null), Is.EqualTo(0));
        }

        /// <summary>Positive control: without it, the "ignored" tests below would pass on a counter stuck at 0.</summary>
        [Test]
        public void CountAlivePlayers_AliveHealthOwner_CountsIt()
        {
            _turnOrder.AddEntity(new HealthOwnerAgent(NewAliveHealth()));

            int count = _turnOrder.CountAlivePlayers();

            Assert.That(count, Is.EqualTo(1));
        }

        /// <summary>Walks the "ho.Health == null" guard: without it this would NullReference on IsAlive.</summary>
        [Test]
        public void CountAlivePlayers_HealthOwnerWithNullHealth_IgnoresIt()
        {
            _turnOrder.AddEntity(new HealthOwnerAgent(NewAliveHealth()));
            _turnOrder.AddEntity(new HealthOwnerAgent(null));

            int count = _turnOrder.CountAlivePlayers();

            Assert.That(count, Is.EqualTo(1));
        }

        /// <summary>The real ship: Health is null and it is not an IHealthOwner, so it never counts as crew.</summary>
        [Test]
        public void CountAlivePlayers_ShipTurnAgent_IgnoresIt()
        {
            ShipTurnAgent ship = NewShip();
            Assert.That(ship.Health, Is.Null, "Precondition: the ship has no health.");
            _turnOrder.AddEntity(new HealthOwnerAgent(NewAliveHealth()));
            _turnOrder.AddEntity(ship);

            int count = _turnOrder.CountAlivePlayers();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void CountAlivePlayers_HostileWithNullHealth_IsNotCountedAsAPlayer()
        {
            _turnOrder.AddEntity(NewHostileWithNullHealth());

            int count = _turnOrder.CountAlivePlayers();

            Assert.That(count, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- CountAliveEnemies

        [Test]
        public void CountAliveEnemies_NullTurnOrder_ReturnsZero()
        {
            Assert.That(TurnOrderQueries.CountAliveEnemies(null), Is.EqualTo(0));
        }

        /// <summary>
        /// Walks the "ho.Health == null" guard on the enemy side: without it this would NullReference.
        ///
        /// There is deliberately no "alive hostile is counted" positive control: HostileCharacter caches its
        /// HealthController only in Awake, and in EditMode Awake does not run (not even on SetActive(true)),
        /// so a hostile with a live Health cannot be built here without a production seam.
        /// </summary>
        [Test]
        public void CountAliveEnemies_HostileWithNullHealth_IgnoresIt()
        {
            _turnOrder.AddEntity(NewHostileWithNullHealth());

            int count = _turnOrder.CountAliveEnemies();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountAliveEnemies_ShipTurnAgent_IgnoresIt()
        {
            _turnOrder.AddEntity(NewShip());

            int count = _turnOrder.CountAliveEnemies();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void CountAliveEnemies_AlivePlayerHealthOwner_IsNotCountedAsAnEnemy()
        {
            _turnOrder.AddEntity(new HealthOwnerAgent(NewAliveHealth()));

            int count = _turnOrder.CountAliveEnemies();

            Assert.That(count, Is.EqualTo(0));
        }
    }
}
