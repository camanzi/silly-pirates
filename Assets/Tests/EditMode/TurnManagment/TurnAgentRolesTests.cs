using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.TurnManagment
{
    /// <summary>
    /// TurnAgentRoles is the single place that turns a tag into a role. Expected values come from its
    /// constants: CrewTag = "Player", ShipTag = "Ship", and HasPlayerAgency is documented as identical to
    /// IsCrewMember today (the ship's turn is automatic). Every predicate is null-safe.
    /// </summary>
    public class TurnAgentRolesTests
    {
        [Test]
        public void Tags_Constants_MatchTheProjectTags()
        {
            Assert.That(TurnAgentRoles.CrewTag, Is.EqualTo("Player"));
            Assert.That(TurnAgentRoles.ShipTag, Is.EqualTo("Ship"));
        }

        [TestCase("Player", true)]
        [TestCase("Ship", false)]
        [TestCase("Untagged", false)]
        public void IsCrewMember_TaggedAgent_MatchesOnlyThePlayerTag(string tag, bool expected)
        {
            FakeTurnAgent agent = new FakeTurnAgent { Tag = tag };

            bool result = TurnAgentRoles.IsCrewMember(agent);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("Player", false)]
        [TestCase("Ship", true)]
        [TestCase("Untagged", false)]
        public void IsShip_TaggedAgent_MatchesOnlyTheShipTag(string tag, bool expected)
        {
            FakeTurnAgent agent = new FakeTurnAgent { Tag = tag };

            bool result = TurnAgentRoles.IsShip(agent);

            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>The ship's turn gives the player no agency: only the crew does.</summary>
        [TestCase("Player", true)]
        [TestCase("Ship", false)]
        [TestCase("Untagged", false)]
        public void HasPlayerAgency_TaggedAgent_IsTrueOnlyForTheCrew(string tag, bool expected)
        {
            FakeTurnAgent agent = new FakeTurnAgent { Tag = tag };

            bool result = TurnAgentRoles.HasPlayerAgency(agent);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void IsCrewMember_NullAgent_ReturnsFalse()
        {
            Assert.That(TurnAgentRoles.IsCrewMember(null), Is.False);
        }

        [Test]
        public void IsShip_NullAgent_ReturnsFalse()
        {
            Assert.That(TurnAgentRoles.IsShip(null), Is.False);
        }

        [Test]
        public void HasPlayerAgency_NullAgent_ReturnsFalse()
        {
            Assert.That(TurnAgentRoles.HasPlayerAgency(null), Is.False);
        }
    }
}
