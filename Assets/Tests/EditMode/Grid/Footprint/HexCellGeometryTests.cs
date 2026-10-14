using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Hex cell geometry. The World half runs against a real Grid component configured like PF_Ship's
    /// (Hexagon layout, cell size 2.5 x 3 x 1, swizzle YZX) on a moved and rotated GameObject, so the
    /// local-to-world step is exercised too. The Planar half needs no Unity object.
    ///
    /// Note on the project cell size: 2.5 x 3 is not a regular hexagon (regular pointy-top needs
    /// width/height = sqrt(3)/2 = 0.866, here 0.833). Unity draws the cell as a hexagon inscribed in the
    /// cell-size box (top/bottom corners at +-y/2, side corners at +-x/2, +-y/4), so its corners are NOT
    /// equidistant from the centre. Equidistance is therefore pinned on a regular cell size, and the project
    /// size is pinned against the box-inscribed hexagon instead.
    /// </summary>
    public class HexCellGeometryTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly Vector3 ProjectCellSize = new Vector3(2.5f, 3f, 1f);
        private static readonly Vector3 RegularCellSize = new Vector3(Mathf.Sqrt(3f), 2f, 1f);

        // Even row, odd row, negative odd rows (y & 1 on two's complement), negative even row.
        private static readonly Vector3Int[] SampleCells =
        {
            new Vector3Int(2, 2, 0),
            new Vector3Int(3, 3, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-2, -3, 0),
            new Vector3Int(-1, -4, 0),
            new Vector3Int(0, 0, 0),
        };

        private GameObject _gridObject;

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
                Object.DestroyImmediate(_gridObject);
        }

        private UnityEngine.Grid CreateGrid(Vector3 cellSize)
        {
            _gridObject = new GameObject("HexCellGeometryTests Grid");
            _gridObject.transform.SetPositionAndRotation(new Vector3(10f, -2f, 4f), Quaternion.Euler(0f, 30f, 0f));
            UnityEngine.Grid grid = _gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            grid.cellSize = cellSize;
            grid.cellGap = Vector3.zero;
            grid.cellSwizzle = GridLayout.CellSwizzle.YZX;
            return grid;
        }

        private static Vector3[] CornersWorld(GridLayout layout, Vector3Int cell)
        {
            Vector3[] corners = new Vector3[HexCellGeometry.CornerCount];
            HexCellGeometry.GetCornersWorld(layout, cell, corners);
            return corners;
        }

        private static Vector2[] CornersPlanar(Vector3Int cell, float size)
        {
            Vector2[] corners = new Vector2[HexCellGeometry.CornerCount];
            HexCellGeometry.GetCornersPlanar(HexCellGeometry.CellCenterPlanar(cell, size), size, corners);
            return corners;
        }

        private static int CountShared(Vector3[] a, Vector3[] b)
        {
            int shared = 0;
            foreach (Vector3 p in a)
                foreach (Vector3 q in b)
                    if (Vector3.Distance(p, q) < Tolerance) shared++;
            return shared;
        }

        private static int CountShared(Vector2[] a, Vector2[] b)
        {
            int shared = 0;
            foreach (Vector2 p in a)
                foreach (Vector2 q in b)
                    if (Vector2.Distance(p, q) < Tolerance) shared++;
            return shared;
        }

        // ================================================================ World

        [Test]
        public void CellCenterWorld_ProjectGrid_EqualsGridGetCellCenterWorld()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3 centre = HexCellGeometry.CellCenterWorld(grid, cell);

                Assert.That(Vector3.Distance(centre, grid.GetCellCenterWorld(cell)), Is.LessThan(Tolerance),
                    $"cell {cell}: {centre} vs {grid.GetCellCenterWorld(cell)}");
            }
        }

        [Test]
        public void GetCornersWorld_RegularCellSize_SixCornersEquidistantFromCentre()
        {
            UnityEngine.Grid grid = CreateGrid(RegularCellSize);
            float radius = RegularCellSize.y / 2f;

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3 centre = HexCellGeometry.CellCenterWorld(grid, cell);
                Vector3[] corners = CornersWorld(grid, cell);

                for (int i = 0; i < corners.Length; i++)
                    Assert.That(Vector3.Distance(corners[i], centre), Is.EqualTo(radius).Within(Tolerance),
                        $"cell {cell} corner {i}");
            }
        }

        /// <summary>
        /// Corner 2 (between NE and NW) is the top point and corner 5 (between SW and SE) the bottom one, at
        /// half the cell height; the four side corners sit at (x/2, y/4) from the centre.
        /// </summary>
        [Test]
        public void GetCornersWorld_ProjectCellSize_CornersMatchTheHexInscribedInTheCellBox()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);
            float pointDistance = ProjectCellSize.y / 2f;
            float sideDistance = new Vector2(ProjectCellSize.x / 2f, ProjectCellSize.y / 4f).magnitude;
            float[] expected = { sideDistance, sideDistance, pointDistance, sideDistance, sideDistance, pointDistance };

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3 centre = HexCellGeometry.CellCenterWorld(grid, cell);
                Vector3[] corners = CornersWorld(grid, cell);

                for (int i = 0; i < corners.Length; i++)
                    Assert.That(Vector3.Distance(corners[i], centre), Is.EqualTo(expected[i]).Within(Tolerance),
                        $"cell {cell} corner {i}");
            }
        }

        [Test]
        public void GetCornersWorld_ProjectCellSize_OppositeCornersAreSymmetricAboutTheCentre()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3 centre = HexCellGeometry.CellCenterWorld(grid, cell);
                Vector3[] corners = CornersWorld(grid, cell);

                for (int i = 0; i < 3; i++)
                    Assert.That(Vector3.Distance((corners[i] + corners[i + 3]) / 2f, centre), Is.LessThan(Tolerance),
                        $"cell {cell} corners {i}/{i + 3}");
            }
        }

        [TestCase(2.5f, 3f)]
        [TestCase(1.7320508f, 2f)]
        public void GetCornersWorld_AdjacentCells_ShareExactlyTwoCorners(float width, float height)
        {
            UnityEngine.Grid grid = CreateGrid(new Vector3(width, height, 1f));

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3[] corners = CornersWorld(grid, cell);

                for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                {
                    Vector3Int neighbour = HexCoordinates.Neighbour(cell, (HexDirection)d);

                    Assert.That(CountShared(corners, CornersWorld(grid, neighbour)), Is.EqualTo(2),
                        $"cell {cell} toward {(HexDirection)d} ({neighbour})");
                }
            }
        }

        [Test]
        public void GetCornersWorld_CellTwoStepsAway_SharesNoCorner()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);
            Vector3Int cell = new Vector3Int(2, 2, 0);
            Vector3Int twoEast = new Vector3Int(4, 2, 0);

            Assert.That(CountShared(CornersWorld(grid, cell), CornersWorld(grid, twoEast)), Is.EqualTo(0));
        }

        [Test]
        public void GetCornersWorld_ArrayShorterThanSix_ThrowsArgumentException()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);

            Assert.That(() => HexCellGeometry.GetCornersWorld(grid, Vector3Int.zero, new Vector3[5]),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void GetCornersWorld_NullLayout_ThrowsArgumentNullException()
        {
            Assert.That(() => HexCellGeometry.GetCornersWorld(null, Vector3Int.zero, new Vector3[6]),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ================================================================ Planar

        [Test]
        public void CellCenterPlanar_Origin_IsZero()
        {
            Vector2 centre = HexCellGeometry.CellCenterPlanar(Vector3Int.zero, 1.3f);

            Assert.That(centre.magnitude, Is.LessThan(Tolerance));
        }

        /// <summary>Odd rows are shifted right by half a cell width (sqrt(3) * size / 2), negative ones too.</summary>
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(-3)]
        public void CellCenterPlanar_OddRow_IsShiftedRightByHalfACell(int row)
        {
            const float size = 1f;
            float halfWidth = Mathf.Sqrt(3f) * size / 2f;

            Vector2 odd = HexCellGeometry.CellCenterPlanar(new Vector3Int(0, row, 0), size);

            Assert.That(odd.x, Is.EqualTo(halfWidth).Within(Tolerance));
        }

        [TestCase(2)]
        [TestCase(-2)]
        public void CellCenterPlanar_EvenRow_IsNotShifted(int row)
        {
            Vector2 even = HexCellGeometry.CellCenterPlanar(new Vector3Int(0, row, 0), 1f);

            Assert.That(even.x, Is.EqualTo(0f).Within(Tolerance));
        }

        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(0, -1)]
        [TestCase(-2, -3)]
        public void CellCenterPlanar_NorthNeighbours_AreDrawnHigher(int x, int y)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);
            Vector2 centre = HexCellGeometry.CellCenterPlanar(cell, 1f);

            Vector2 northEast = HexCellGeometry.CellCenterPlanar(HexCoordinates.Neighbour(cell, HexDirection.NorthEast), 1f);
            Vector2 northWest = HexCellGeometry.CellCenterPlanar(HexCoordinates.Neighbour(cell, HexDirection.NorthWest), 1f);

            Assert.That(northEast.y, Is.GreaterThan(centre.y));
            Assert.That(northWest.y, Is.GreaterThan(centre.y));
        }

        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(0, -1)]
        [TestCase(-2, -3)]
        public void CellCenterPlanar_EastNeighbour_IsDrawnToTheRightOnTheSameRow(int x, int y)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);
            Vector2 centre = HexCellGeometry.CellCenterPlanar(cell, 1f);

            Vector2 east = HexCellGeometry.CellCenterPlanar(HexCoordinates.Neighbour(cell, HexDirection.East), 1f);

            Assert.That(east.y, Is.EqualTo(centre.y).Within(Tolerance));
            Assert.That(east.x, Is.GreaterThan(centre.x));
        }

        [Test]
        public void CellCenterPlanar_AnyNeighbour_IsSqrt3TimesSizeAway()
        {
            const float size = 0.8f;

            foreach (Vector3Int cell in SampleCells)
            {
                Vector2 centre = HexCellGeometry.CellCenterPlanar(cell, size);

                for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                {
                    Vector2 neighbour = HexCellGeometry.CellCenterPlanar(HexCoordinates.Neighbour(cell, (HexDirection)d), size);

                    Assert.That(Vector2.Distance(centre, neighbour), Is.EqualTo(Mathf.Sqrt(3f) * size).Within(Tolerance),
                        $"cell {cell} toward {(HexDirection)d}");
                }
            }
        }

        [Test]
        public void GetCornersPlanar_AnyCentre_SixCornersAtRadiusSize()
        {
            const float size = 1.7f;
            Vector2 centre = new Vector2(-3f, 5f);
            Vector2[] corners = new Vector2[6];

            HexCellGeometry.GetCornersPlanar(centre, size, corners);

            for (int i = 0; i < corners.Length; i++)
                Assert.That(Vector2.Distance(corners[i], centre), Is.EqualTo(size).Within(Tolerance), $"corner {i}");
        }

        [Test]
        public void GetCornersPlanar_AdjacentCells_ShareExactlyTwoCorners()
        {
            const float size = 1f;

            foreach (Vector3Int cell in SampleCells)
            {
                Vector2[] corners = CornersPlanar(cell, size);

                for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                {
                    Vector3Int neighbour = HexCoordinates.Neighbour(cell, (HexDirection)d);

                    Assert.That(CountShared(corners, CornersPlanar(neighbour, size)), Is.EqualTo(2),
                        $"cell {cell} toward {(HexDirection)d} ({neighbour})");
                }
            }
        }

        /// <summary>Corner i sits between neighbour i-1 and neighbour i: it is a corner of all three cells.</summary>
        [Test]
        public void GetCornersPlanar_CornerI_IsEquidistantFromCentreAndNeighboursIMinusOneAndI()
        {
            const float size = 1f;

            foreach (Vector3Int cell in SampleCells)
            {
                Vector2[] corners = CornersPlanar(cell, size);

                for (int i = 0; i < HexCellGeometry.CornerCount; i++)
                {
                    Vector3Int previous = HexCoordinates.Neighbour(cell, (HexDirection)((i + 5) % 6));
                    Vector3Int current = HexCoordinates.Neighbour(cell, (HexDirection)i);

                    Assert.That(Vector2.Distance(corners[i], HexCellGeometry.CellCenterPlanar(previous, size)),
                        Is.EqualTo(size).Within(Tolerance), $"cell {cell} corner {i} vs neighbour {i - 1}");
                    Assert.That(Vector2.Distance(corners[i], HexCellGeometry.CellCenterPlanar(current, size)),
                        Is.EqualTo(size).Within(Tolerance), $"cell {cell} corner {i} vs neighbour {i}");
                }
            }
        }

        /// <summary>Same neighbour order as the world corners: corner i flanks neighbours i-1 and i.</summary>
        [Test]
        public void GetCornersWorld_CornerI_IsSharedWithNeighboursIMinusOneAndI()
        {
            UnityEngine.Grid grid = CreateGrid(ProjectCellSize);

            foreach (Vector3Int cell in SampleCells)
            {
                Vector3[] corners = CornersWorld(grid, cell);

                for (int i = 0; i < HexCellGeometry.CornerCount; i++)
                {
                    Vector3[] previous = CornersWorld(grid, HexCoordinates.Neighbour(cell, (HexDirection)((i + 5) % 6)));
                    Vector3[] current = CornersWorld(grid, HexCoordinates.Neighbour(cell, (HexDirection)i));

                    Assert.That(CountShared(new[] { corners[i] }, previous), Is.EqualTo(1),
                        $"cell {cell} corner {i} vs neighbour {i - 1}");
                    Assert.That(CountShared(new[] { corners[i] }, current), Is.EqualTo(1),
                        $"cell {cell} corner {i} vs neighbour {i}");
                }
            }
        }

        [Test]
        public void GetCornersPlanar_ArrayShorterThanSix_ThrowsArgumentException()
        {
            Assert.That(() => HexCellGeometry.GetCornersPlanar(Vector2.zero, 1f, new Vector2[5]),
                Throws.TypeOf<ArgumentException>());
        }
    }
}
