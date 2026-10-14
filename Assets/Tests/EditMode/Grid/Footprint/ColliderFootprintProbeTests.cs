using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// The physics adapter against a real Grid and real BoxColliders. The grid has a REGULAR cell size
    /// (sqrt(3) x 2, hex radius 1) with the ship's YZX swizzle, on a moved and rotated GameObject, so the
    /// geometry reasoning of FootprintRasterizerTests holds in world units:
    /// a box 1 wide and 3 * sqrt(3) long centred on a cell covers 7 of the 13 samples of each neighbour along
    /// its axis (>= 0.5) and none of any other cell. Boxes are oriented from the grid itself (neighbour centres
    /// and HexCellGeometry.PlaneNormalWorld), never from a guessed world axis.
    /// Collider.Raycast is not a scene query and the probe calls Physics.SyncTransforms itself, so this runs
    /// deterministically in EditMode.
    /// </summary>
    public class ColliderFootprintProbeTests : ScriptableObjectFixture
    {
        private static readonly float Sqrt3 = Mathf.Sqrt(3f);

        private UnityEngine.Grid _grid;

        [SetUp]
        public void SetUp()
        {
            GameObject gridObject = Track(new GameObject("ColliderFootprintProbeTests Grid"));
            gridObject.transform.SetPositionAndRotation(new Vector3(7f, -1f, 3f), Quaternion.Euler(0f, 25f, 0f));
            _grid = gridObject.AddComponent<UnityEngine.Grid>();
            _grid.cellLayout = GridLayout.CellLayout.Hexagon;
            _grid.cellSize = new Vector3(Sqrt3, 2f, 1f);
            _grid.cellGap = Vector3.zero;
            _grid.cellSwizzle = GridLayout.CellSwizzle.YZX;
        }

        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private Vector3 Centre(Vector3Int cell) => HexCellGeometry.CellCenterWorld(_grid, cell);

        private Vector3 Normal(Vector3Int cell) => HexCellGeometry.PlaneNormalWorld(_grid, cell);

        private Vector3 Towards(Vector3Int cell, HexDirection direction)
            => (Centre(HexCoordinates.Neighbour(cell, direction)) - Centre(cell)).normalized;

        /// <summary>An element root whose pivot sits on the centre of <paramref name="cell"/>, upright on the grid.</summary>
        private GameObject NewElementRoot(Vector3Int cell)
        {
            GameObject root = Track(new GameObject("element"));
            root.transform.SetPositionAndRotation(Centre(cell),
                Quaternion.LookRotation(Towards(cell, HexDirection.East), Normal(cell)));
            return root;
        }

        /// <summary>
        /// A child box standing on the grid plane over <paramref name="cell"/>: local z along
        /// <paramref name="axis"/> (length), local y along the plane normal (height 1), local x across (width).
        /// </summary>
        private BoxCollider AddBox(GameObject root, Vector3Int cell, Vector3 axis, float length, float width)
        {
            GameObject box = new GameObject("box");
            box.transform.SetParent(root.transform, true);
            Vector3 normal = Normal(cell);
            box.transform.SetPositionAndRotation(Centre(cell) + normal * 0.5f, Quaternion.LookRotation(axis, normal));
            BoxCollider collider = box.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, 1f, length);
            return collider;
        }

        /// <summary>The 3-cell bar: 1 wide, 3 * sqrt(3) long.</summary>
        private BoxCollider AddBar(GameObject root, Vector3Int cell, Vector3 axis)
            => AddBox(root, cell, axis, 3f * Sqrt3, 1f);

        /// <summary>A square that covers every sample of its own cell and no sample of any neighbour.</summary>
        private BoxCollider AddCellBox(GameObject root, Vector3Int cell)
            => AddBox(root, cell, Towards(cell, HexDirection.East), 1.6f, 1.6f);

        private List<Vector3Int> Probe(GameObject root, Vector3Int anchor, out bool found)
        {
            List<Vector3Int> results = new List<Vector3Int>();
            found = ColliderFootprintProbe.Probe(root.transform, _grid, anchor, results);
            return results;
        }

        // ---------------------------------------------------------------- grid sanity (guards the geometry below)

        [Test]
        public void TestGrid_NeighbourCentres_AreSqrt3Apart()
        {
            Vector3Int anchor = Cell(2, 3);

            float east = Vector3.Distance(Centre(anchor), Centre(HexCoordinates.Neighbour(anchor, HexDirection.East)));
            float northEast = Vector3.Distance(Centre(anchor), Centre(HexCoordinates.Neighbour(anchor, HexDirection.NorthEast)));

            Assert.That(east, Is.EqualTo(Sqrt3).Within(1e-4f));
            Assert.That(northEast, Is.EqualTo(Sqrt3).Within(1e-4f));
        }

        // ---------------------------------------------------------------- shapes

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void Probe_BoxOverOneCell_ReturnsThatCellOnly(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, anchor);

            List<Vector3Int> results = Probe(root, anchor, out bool found);

            Assert.That(found, Is.True);
            Assert.That(results, Is.EqualTo(new[] { anchor }));
        }

        [Test]
        public void Probe_BoxOverAnotherCell_ReturnsTheAnchorFirstThenThatCell()
        {
            Vector3Int anchor = Cell(2, 2);
            Vector3Int covered = Cell(5, 4);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, covered);

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results, Is.EqualTo(new[] { anchor, covered }));
        }

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void Probe_BarAlongEast_ReturnsTheAnchorAndItsEastAndWestNeighbours(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);
            GameObject root = NewElementRoot(anchor);
            AddBar(root, anchor, Towards(anchor, HexDirection.East));

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[]
            {
                anchor,
                HexCoordinates.Neighbour(anchor, HexDirection.East),
                HexCoordinates.Neighbour(anchor, HexDirection.West),
            }));
        }

        /// <summary>
        /// The same element turned by the angle between the East and NorthEast neighbours (60 degrees on a
        /// regular grid) about the plane normal: the footprint follows the rotation.
        /// </summary>
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void Probe_BarRotatedFromEastToNorthEast_ReturnsTheAnchorAndItsNorthEastAndSouthWestNeighbours(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);
            GameObject root = NewElementRoot(anchor);
            AddBar(root, anchor, Towards(anchor, HexDirection.East));
            Vector3 normal = Normal(anchor);
            float angle = Vector3.SignedAngle(Towards(anchor, HexDirection.East), Towards(anchor, HexDirection.NorthEast), normal);

            root.transform.rotation = Quaternion.AngleAxis(angle, normal) * root.transform.rotation;
            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(Mathf.Abs(angle), Is.EqualTo(60f).Within(1e-2f));
            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[]
            {
                anchor,
                HexCoordinates.Neighbour(anchor, HexDirection.NorthEast),
                HexCoordinates.Neighbour(anchor, HexDirection.SouthWest),
            }));
        }

        /// <summary>Two child boxes on cells far apart: a disconnected footprint is allowed.</summary>
        [Test]
        public void Probe_TwoBoxesOnDistantCells_ReturnsBothPlusTheAnchor()
        {
            Vector3Int anchor = Cell(0, 0);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, Cell(4, 1));
            AddCellBox(root, Cell(-3, -2));

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[] { anchor, Cell(4, 1), Cell(-3, -2) }));
        }

        [Test]
        public void Probe_AnyFootprint_EveryCellIsOnTheAnchorLayer()
        {
            Vector3Int anchor = new Vector3Int(2, 2, 0);
            GameObject root = NewElementRoot(anchor);
            AddBar(root, anchor, Towards(anchor, HexDirection.East));

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results.ConvertAll(c => c.z), Has.All.EqualTo(anchor.z));
        }

        // ---------------------------------------------------------------- which colliders count

        [Test]
        public void Probe_OnlyColliderDisabled_ReturnsFalseAndTheAnchorAlone()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            AddBar(root, anchor, Towards(anchor, HexDirection.East)).enabled = false;

            List<Vector3Int> results = Probe(root, anchor, out bool found);

            Assert.That(found, Is.False);
            Assert.That(results, Is.EqualTo(new[] { anchor }));
        }

        [Test]
        public void Probe_OnlyColliderIsATrigger_ReturnsFalseAndTheAnchorAlone()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            AddBar(root, anchor, Towards(anchor, HexDirection.East)).isTrigger = true;

            List<Vector3Int> results = Probe(root, anchor, out bool found);

            Assert.That(found, Is.False);
            Assert.That(results, Is.EqualTo(new[] { anchor }));
        }

        [Test]
        public void Probe_DisabledColliderNextToAnEnabledOne_OnlyTheEnabledOneCounts()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, Cell(4, 2));
            AddCellBox(root, Cell(0, 2)).enabled = false;

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results, Is.EqualTo(new[] { anchor, Cell(4, 2) }));
        }

        [Test]
        public void Probe_NestedGridElementWithItsOwnCollider_IsNotPartOfTheFootprint()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, Cell(4, 2));
            BoxCollider nestedBox = AddCellBox(root, Cell(0, 2));
            nestedBox.gameObject.AddComponent<GridElement>();

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results, Is.EqualTo(new[] { anchor, Cell(4, 2) }));
        }

        [Test]
        public void Probe_NoColliderAtAll_ReturnsFalseAndTheAnchorAlone()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);

            List<Vector3Int> results = Probe(root, anchor, out bool found);

            Assert.That(found, Is.False);
            Assert.That(results, Is.EqualTo(new[] { anchor }));
        }

        // ---------------------------------------------------------------- transforms moved without a physics step

        /// <summary>
        /// Auto Sync Transforms is off in the project: the probe must sync itself, or a box moved after its
        /// collider was last synced would be sampled where it used to be.
        /// </summary>
        [Test]
        public void Probe_BoxMovedAfterTheLastSync_SamplesTheNewPosition()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            BoxCollider box = AddCellBox(root, Cell(4, 2));
            Physics.SyncTransforms();
            box.transform.position += Centre(Cell(4, 4)) - Centre(Cell(4, 2));

            List<Vector3Int> results = Probe(root, anchor, out _);

            Assert.That(results, Is.EqualTo(new[] { anchor, Cell(4, 4) }));
        }

        // ---------------------------------------------------------------- cap

        [Test]
        public void Probe_ColliderCoveringMoreThanTheCap_TruncatesToMaxCellsWithAWarning()
        {
            Vector3Int anchor = Cell(0, 0);
            GameObject root = NewElementRoot(anchor);
            AddBox(root, anchor, Towards(anchor, HexDirection.East), 30f, 30f);
            LogAssert.Expect(LogType.Warning, new Regex("truncated"));

            List<Vector3Int> results = Probe(root, anchor, out bool found);

            Assert.That(found, Is.True);
            Assert.That(results, Has.Count.EqualTo(ColliderFootprintProbe.MaxCells));
            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.Unique);
        }

        [Test]
        public void Probe_NonEmptyResults_ClearsThemFirst()
        {
            Vector3Int anchor = Cell(2, 2);
            GameObject root = NewElementRoot(anchor);
            AddCellBox(root, anchor);
            List<Vector3Int> results = new List<Vector3Int> { Cell(9, 9), Cell(8, 8) };

            ColliderFootprintProbe.Probe(root.transform, _grid, anchor, results);

            Assert.That(results, Is.EqualTo(new[] { anchor }));
        }
    }
}
