using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Objects
{
    /// <summary>
    /// Multi-cell occupancy of GridElement. Elements live on inactive GameObjects (no floor raycast, no
    /// LogError) and talk to a fresh GridStateDataSO per test, never the project asset. The private
    /// serialized fields are injected through SerializedObject, the same path the Inspector uses.
    /// OnDisable is invoked by reflection (see InvokeOnDisable for why).
    /// </summary>
    public class GridElementTests : ScriptableObjectFixture
    {
        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private GridStateDataSO _gridState;

        [SetUp]
        public void SetUp() => _gridState = NewSO<GridStateDataSO>();

        private T NewElement<T>(GridFootprint footprint) where T : GridElement
        {
            T element = NewInactiveGameObject(typeof(T).Name).AddComponent<T>();
            SerializedObject so = new SerializedObject(element);
            so.FindProperty("_gridStateData").objectReferenceValue = _gridState;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetFootprint(element, footprint);
            return element;
        }

        private static void SetFootprint(GridElement element, GridFootprint footprint)
        {
            SerializedObject so = new SerializedObject(element);
            so.FindProperty("_footprint._shape").intValue = (int)footprint.Shape;
            so.FindProperty("_footprint._count").intValue = footprint.Count;
            so.FindProperty("_footprint._direction").intValue = (int)footprint.Direction;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Runs the real GridElement.OnDisable. EditMode sends no lifecycle messages to a MonoBehaviour without
        /// [ExecuteAlways] (verified: SetActive(true/false) left every cell registered), and a test subclass
        /// cannot be attached because this assembly is editor-only. Reflection is the remaining route without
        /// touching runtime code; the null check turns a rename into a loud failure instead of a silent pass.
        /// </summary>
        private static void InvokeOnDisable(GridElement element)
        {
            MethodInfo onDisable = typeof(GridElement).GetMethod("OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(onDisable, Is.Not.Null, "GridElement.OnDisable not found: was it renamed?");
            onDisable.Invoke(element, null);
        }

        private static GridFootprint Line3East => new GridFootprint(FootprintShape.Line, 3, HexDirection.East);

        private static List<Vector3Int> Resolve(Vector3Int anchor, GridFootprint footprint)
        {
            List<Vector3Int> cells = new List<Vector3Int>();
            HexFootprint.Resolve(anchor, footprint, cells);
            return cells;
        }

        [Test]
        public void AddComponent_DefaultFootprint_IsSingleOfOneCell()
        {
            GridElement element = NewInactiveGameObject("element").AddComponent<GridElement>();

            Assert.That(element.Footprint.Shape, Is.EqualTo(FootprintShape.Single));
            Assert.That(element.Footprint.Count, Is.EqualTo(1));
        }

        [Test]
        public void SetGridPosition_SingleFootprint_RegistersOnlyTheAnchor()
        {
            GridElement element = NewElement<GridElement>(GridFootprint.Single);

            element.gridPosition = Cell(2, 2);

            Assert.That(element.OccupiedCells, Is.EqualTo(new[] { Cell(2, 2) }));
            Assert.That(_gridState.IsOccupied(Cell(2, 2), element), Is.True);
        }

        [Test]
        public void SetGridPosition_LineFootprint_OccupiedCellsEqualResolve()
        {
            GridElement element = NewElement<GridElement>(Line3East);

            element.gridPosition = Cell(2, 2);

            Assert.That(element.OccupiedCells, Is.EqualTo(Resolve(Cell(2, 2), Line3East)));
        }

        [Test]
        public void SetGridPosition_LineFootprint_RegistersEveryResolvedCell()
        {
            GridElement element = NewElement<GridElement>(Line3East);

            element.gridPosition = Cell(2, 2);

            foreach (Vector3Int cell in new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) })
                Assert.That(_gridState.IsOccupied(cell, element), Is.True, $"cell {cell}");
        }

        [Test]
        public void SetGridPosition_ClusterFootprint_RegistersEveryResolvedCell()
        {
            GridFootprint cluster = new GridFootprint(FootprintShape.Cluster, 7, HexDirection.East);
            GridElement element = NewElement<GridElement>(cluster);

            element.gridPosition = Cell(3, 3);

            foreach (Vector3Int cell in Resolve(Cell(3, 3), cluster))
                Assert.That(_gridState.IsOccupied(cell, element), Is.True, $"cell {cell}");
        }

        [Test]
        public void SetGridPosition_Moving_FreesEveryOldCell()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            element.gridPosition = Cell(2, 6);

            foreach (Vector3Int cell in new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) })
                Assert.That(_gridState.IsOccupied(cell), Is.False, $"old cell {cell}");
        }

        [Test]
        public void SetGridPosition_Moving_OccupiesEveryNewCell()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            element.gridPosition = Cell(2, 6);

            Assert.That(element.OccupiedCells, Is.EquivalentTo(new[] { Cell(1, 6), Cell(2, 6), Cell(3, 6) }));
            foreach (Vector3Int cell in element.OccupiedCells)
                Assert.That(_gridState.IsOccupied(cell, element), Is.True, $"new cell {cell}");
        }

        /// <summary>Overlapping move: the cells shared by old and new footprint must stay registered.</summary>
        [Test]
        public void SetGridPosition_MovingOneStepAlongTheLine_KeepsSharedCellsOccupied()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            element.gridPosition = Cell(3, 2);

            Assert.That(_gridState.IsOccupied(Cell(1, 2)), Is.False);
            foreach (Vector3Int cell in new[] { Cell(2, 2), Cell(3, 2), Cell(4, 2) })
                Assert.That(_gridState.IsOccupied(cell, element), Is.True, $"cell {cell}");
        }

        [Test]
        public void OnDisable_AfterRegistration_FreesEveryCell()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            InvokeOnDisable(element);

            foreach (Vector3Int cell in new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) })
                Assert.That(_gridState.IsOccupied(cell), Is.False, $"cell {cell}");
            Assert.That(element.OccupiedCells, Is.Empty);
        }

        [Test]
        public void OnDisable_OtherOccupantOnASharedCell_LeavesThatOccupantRegistered()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            GridElement other = NewElement<GridElement>(GridFootprint.Single);
            element.gridPosition = Cell(2, 2);
            other.gridPosition = Cell(3, 2);

            InvokeOnDisable(element);

            Assert.That(_gridState.IsOccupied(Cell(3, 2), other), Is.True);
            Assert.That(_gridState.IsOccupied(Cell(3, 2), element), Is.False);
        }

        /// <summary>
        /// Unregistering must use the cells actually registered, not a fresh Resolve of the current footprint:
        /// shrinking Line 3 to Single would otherwise leave (1,2) and (3,2) occupied forever.
        /// </summary>
        [Test]
        public void OnDisable_FootprintChangedAfterRegistration_FreesTheOriginallyRegisteredCells()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);
            SetFootprint(element, GridFootprint.Single);

            InvokeOnDisable(element);

            foreach (Vector3Int cell in new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) })
                Assert.That(_gridState.IsOccupied(cell), Is.False, $"cell {cell}");
        }

        [Test]
        public void SetGridPosition_FootprintChangedAfterRegistration_FreesOldCellsAndRegistersTheNewShape()
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);
            SetFootprint(element, GridFootprint.Single);

            element.gridPosition = Cell(2, 6);

            foreach (Vector3Int cell in new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) })
                Assert.That(_gridState.IsOccupied(cell), Is.False, $"old cell {cell}");
            Assert.That(element.OccupiedCells, Is.EqualTo(new[] { Cell(2, 6) }));
        }

        [TestCase(1, 2, true)]
        [TestCase(2, 2, true)]
        [TestCase(3, 2, true)]
        [TestCase(4, 2, false)]
        [TestCase(2, 3, false)]
        public void Occupies_Line3EastAt2_2_MatchesTheResolvedCells(int x, int y, bool expected)
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            Assert.That(element.Occupies(Cell(x, y)), Is.EqualTo(expected));
        }

        /// <summary>
        /// Distances measured from the closest of (1,2),(2,2),(3,2). (5,2) is 3 from the anchor but 2 from the
        /// east end; (0,2) is 1 from the west end.
        /// </summary>
        [TestCase(2, 2, 0)]
        [TestCase(3, 2, 0)]
        [TestCase(5, 2, 2)]
        [TestCase(0, 2, 1)]
        [TestCase(3, 4, 2)]
        public void DistanceTo_Line3EastAt2_2_ReturnsDistanceFromTheClosestCell(int x, int y, int expected)
        {
            GridElement element = NewElement<GridElement>(Line3East);
            element.gridPosition = Cell(2, 2);

            Assert.That(element.DistanceTo(Cell(x, y)), Is.EqualTo(expected));
        }

        [Test]
        public void DistanceTo_NothingRegistered_FallsBackToGridPosition()
        {
            GridElement element = NewElement<GridElement>(Line3East);

            int distance = element.DistanceTo(Cell(3, 0));

            // gridPosition defaults to (0,0,0); (3,0) is three steps east along the same row.
            Assert.That(distance, Is.EqualTo(3));
        }
    }
}
