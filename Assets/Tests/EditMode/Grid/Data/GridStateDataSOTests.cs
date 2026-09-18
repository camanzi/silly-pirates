using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Data
{
    /// <summary>
    /// Cell occupancy. The entities are real GridElement components, but created on inactive GameObjects
    /// on purpose: GridElement.OnEnable raycasts for a floor tilemap and logs an error when it finds none,
    /// which has nothing to do with what is under test here.
    /// </summary>
    public class GridStateDataSOTests : ScriptableObjectFixture
    {
        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private GridStateDataSO _gridState;

        [SetUp]
        public void SetUp() => _gridState = NewSO<GridStateDataSO>();

        private GridElement NewElement(string name) =>
            NewInactiveGameObject(name).AddComponent<GridElement>();

        [Test]
        public void IsOccupied_EmptyCell_ReturnsFalse()
        {
            Assert.That(_gridState.IsOccupied(Cell(0, 0)), Is.False);
        }

        [Test]
        public void RegisterOccupancy_NewCell_MarksCellOccupied()
        {
            GridElement element = NewElement("element");

            _gridState.RegisterOccupancy(Cell(1, 2), element);

            Assert.That(_gridState.IsOccupied(Cell(1, 2)), Is.True);
        }

        [Test]
        public void IsOccupied_WithTheRegisteredEntity_ReturnsTrue()
        {
            GridElement element = NewElement("element");
            _gridState.RegisterOccupancy(Cell(1, 2), element);

            Assert.That(_gridState.IsOccupied(Cell(1, 2), element), Is.True);
        }

        /// <summary>The entity overload asks "is THIS entity here", not "is anybody here".</summary>
        [Test]
        public void IsOccupied_WithADifferentEntityOnTheSameCell_ReturnsFalse()
        {
            GridElement occupant = NewElement("occupant");
            GridElement other = NewElement("other");
            _gridState.RegisterOccupancy(Cell(1, 2), occupant);

            Assert.That(_gridState.IsOccupied(Cell(1, 2), other), Is.False);
        }

        [Test]
        public void RegisterOccupancy_SameEntityTwice_StoresItOnce()
        {
            GridElement element = NewElement("element");

            _gridState.RegisterOccupancy(Cell(0, 0), element);
            _gridState.RegisterOccupancy(Cell(0, 0), element);

            Assert.That(_gridState.GetEntityAt(Cell(0, 0)), Has.Count.EqualTo(1));
        }

        [Test]
        public void RegisterOccupancy_TwoEntitiesOnTheSameCell_ReportsBoth()
        {
            GridElement first = NewElement("first");
            GridElement second = NewElement("second");

            _gridState.RegisterOccupancy(Cell(0, 0), first);
            _gridState.RegisterOccupancy(Cell(0, 0), second);

            HashSet<GridElement> occupants = _gridState.GetEntityAt(Cell(0, 0));
            Assert.That(occupants, Has.Count.EqualTo(2));
            Assert.That(occupants, Does.Contain(first));
            Assert.That(occupants, Does.Contain(second));
        }

        [Test]
        public void UnregisterOccupancy_OneOfTwoEntities_LeavesTheCellOccupied()
        {
            GridElement leaving = NewElement("leaving");
            GridElement staying = NewElement("staying");
            _gridState.RegisterOccupancy(Cell(0, 0), leaving);
            _gridState.RegisterOccupancy(Cell(0, 0), staying);

            _gridState.UnregisterOccupancy(Cell(0, 0), leaving);

            Assert.That(_gridState.IsOccupied(Cell(0, 0)), Is.True);
            Assert.That(_gridState.GetEntityAt(Cell(0, 0)), Has.Count.EqualTo(1));
            Assert.That(_gridState.IsOccupied(Cell(0, 0), staying), Is.True);
        }

        /// <summary>
        /// The last occupant leaving removes the whole entry, so GetEntityAt returns null rather than an
        /// empty set. Callers must null-check it — MoveAbility's walkability predicate does.
        /// </summary>
        [Test]
        public void UnregisterOccupancy_LastEntity_RemovesTheCellEntry()
        {
            GridElement element = NewElement("element");
            _gridState.RegisterOccupancy(Cell(0, 0), element);

            _gridState.UnregisterOccupancy(Cell(0, 0), element);

            Assert.That(_gridState.IsOccupied(Cell(0, 0)), Is.False);
            Assert.That(_gridState.GetEntityAt(Cell(0, 0)), Is.Null);
        }

        [Test]
        public void UnregisterOccupancy_UnknownCell_DoesNotThrow()
        {
            GridElement element = NewElement("element");

            Assert.That(() => _gridState.UnregisterOccupancy(Cell(7, 7), element), Throws.Nothing);
        }

        [Test]
        public void GetEntityAt_EmptyCell_ReturnsNull()
        {
            Assert.That(_gridState.GetEntityAt(Cell(3, 3)), Is.Null);
        }

        [Test]
        public void ClearAll_PopulatedGrid_LeavesNoOccupiedCell()
        {
            _gridState.RegisterOccupancy(Cell(0, 0), NewElement("a"));
            _gridState.RegisterOccupancy(Cell(1, 1), NewElement("b"));

            _gridState.ClearAll();

            Assert.That(_gridState.IsOccupied(Cell(0, 0)), Is.False);
            Assert.That(_gridState.IsOccupied(Cell(1, 1)), Is.False);
        }

        /// <summary>
        /// The ICombatSessionResettable contract: stale occupants from a previous combat would make cells
        /// read as occupied forever, since IsOccupied is a dictionary lookup and not a Unity null check.
        /// </summary>
        [Test]
        public void ResetForNewCombat_PopulatedGrid_LeavesNoOccupiedCell()
        {
            _gridState.RegisterOccupancy(Cell(0, 0), NewElement("a"));

            _gridState.ResetForNewCombat();

            Assert.That(_gridState.IsOccupied(Cell(0, 0)), Is.False);
        }
    }
}
