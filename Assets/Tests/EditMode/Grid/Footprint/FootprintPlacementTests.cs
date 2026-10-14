using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Placement validation of a footprint against two delegate rules (walkable, taken by another element).
    /// The grid is a plain HashSet written inline: no Tilemap, no GridStateDataSO.
    /// </summary>
    public class FootprintPlacementTests
    {
        private static Vector3Int Cell(int x, int y, int z = 0) => new Vector3Int(x, y, z);

        // A row of three on even row 2, anchor first as GridElement produces it.
        private static readonly Vector3Int Anchor = Cell(2, 2);
        private static readonly Vector3Int[] Line3East = { Cell(2, 2), Cell(1, 2), Cell(3, 2) };

        private static Func<Vector3Int, bool> InSet(params Vector3Int[] cells)
        {
            HashSet<Vector3Int> set = new HashSet<Vector3Int>(cells);
            return set.Contains;
        }

        private static Func<Vector3Int, bool> AllWalkable => _ => true;
        private static Func<Vector3Int, bool> NothingTaken => _ => false;

        private static FootprintCellStatus StatusOf(List<FootprintCell> results, Vector3Int cell)
        {
            foreach (FootprintCell result in results)
                if (result.Cell == cell) return result.Status;

            Assert.Fail($"cell {cell} missing from results");
            return default;
        }

        // ---------------------------------------------------------------- return value and statuses

        [Test]
        public void Evaluate_AllCellsWalkableAndFree_ReturnsTrue()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            bool valid = FootprintPlacement.Evaluate(Line3East, AllWalkable, NothingTaken, results);

            Assert.That(valid, Is.True);
        }

        [Test]
        public void Evaluate_AllCellsWalkableAndFree_EveryCellIsValid()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            FootprintPlacement.Evaluate(Line3East, AllWalkable, NothingTaken, results);

            Assert.That(results.ConvertAll(r => r.Status), Has.All.EqualTo(FootprintCellStatus.Valid));
        }

        [Test]
        public void Evaluate_OneCellNotWalkable_ReturnsFalse()
        {
            List<FootprintCell> results = new List<FootprintCell>();
            Func<Vector3Int, bool> walkable = InSet(Cell(1, 2), Cell(2, 2));

            bool valid = FootprintPlacement.Evaluate(Line3East, walkable, NothingTaken, results);

            Assert.That(valid, Is.False);
        }

        [Test]
        public void Evaluate_OneCellNotWalkable_MarksOnlyThatCellNotWalkable()
        {
            List<FootprintCell> results = new List<FootprintCell>();
            Func<Vector3Int, bool> walkable = InSet(Cell(1, 2), Cell(2, 2));

            FootprintPlacement.Evaluate(Line3East, walkable, NothingTaken, results);

            Assert.That(StatusOf(results, Cell(3, 2)), Is.EqualTo(FootprintCellStatus.NotWalkable));
            Assert.That(StatusOf(results, Cell(1, 2)), Is.EqualTo(FootprintCellStatus.Valid));
            Assert.That(StatusOf(results, Cell(2, 2)), Is.EqualTo(FootprintCellStatus.Valid));
        }

        [Test]
        public void Evaluate_OneCellTakenByOther_ReturnsFalse()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            bool valid = FootprintPlacement.Evaluate(Line3East, AllWalkable, InSet(Cell(1, 2)), results);

            Assert.That(valid, Is.False);
        }

        [Test]
        public void Evaluate_OneCellTakenByOther_MarksOnlyThatCellOverlap()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            FootprintPlacement.Evaluate(Line3East, AllWalkable, InSet(Cell(1, 2)), results);

            Assert.That(StatusOf(results, Cell(1, 2)), Is.EqualTo(FootprintCellStatus.Overlap));
            Assert.That(StatusOf(results, Cell(2, 2)), Is.EqualTo(FootprintCellStatus.Valid));
            Assert.That(StatusOf(results, Cell(3, 2)), Is.EqualTo(FootprintCellStatus.Valid));
        }

        [Test]
        public void Evaluate_CellBothNotWalkableAndTaken_ReportsNotWalkable()
        {
            List<FootprintCell> results = new List<FootprintCell>();
            Func<Vector3Int, bool> walkable = InSet(Cell(1, 2), Cell(2, 2));
            Func<Vector3Int, bool> taken = InSet(Cell(3, 2));

            FootprintPlacement.Evaluate(Line3East, walkable, taken, results);

            Assert.That(StatusOf(results, Cell(3, 2)), Is.EqualTo(FootprintCellStatus.NotWalkable));
        }

        [Test]
        public void Evaluate_MixedProblems_EachCellGetsItsOwnStatus()
        {
            List<FootprintCell> results = new List<FootprintCell>();
            Func<Vector3Int, bool> walkable = InSet(Cell(1, 2), Cell(2, 2));
            Func<Vector3Int, bool> taken = InSet(Cell(1, 2));

            FootprintPlacement.Evaluate(Line3East, walkable, taken, results);

            Assert.That(results, Is.EqualTo(new[]
            {
                new FootprintCell(Cell(2, 2), FootprintCellStatus.Valid),
                new FootprintCell(Cell(1, 2), FootprintCellStatus.Overlap),
                new FootprintCell(Cell(3, 2), FootprintCellStatus.NotWalkable),
            }));
        }

        // ---------------------------------------------------------------- results list

        [Test]
        public void Evaluate_NonEmptyResults_ClearsThemFirst()
        {
            List<FootprintCell> results = new List<FootprintCell>
            {
                new FootprintCell(Cell(9, 9), FootprintCellStatus.Overlap),
                new FootprintCell(Cell(8, 8), FootprintCellStatus.NotWalkable),
                new FootprintCell(Cell(7, 7), FootprintCellStatus.Valid),
                new FootprintCell(Cell(6, 6), FootprintCellStatus.Valid),
            };

            FootprintPlacement.Evaluate(new[] { Anchor }, AllWalkable, NothingTaken, results);

            Assert.That(results.ConvertAll(r => r.Cell), Is.EqualTo(new[] { Anchor }));
        }

        [Test]
        public void Evaluate_AnyCells_KeepsTheInputOrder()
        {
            Vector3Int[] cells = { Cell(5, 1, 3), Cell(9, 9, 3), Cell(4, 1, 3), Cell(-3, -3, 3) };
            List<FootprintCell> results = new List<FootprintCell>();

            FootprintPlacement.Evaluate(cells, AllWalkable, NothingTaken, results);

            Assert.That(results.ConvertAll(r => r.Cell), Is.EqualTo(cells));
        }

        [Test]
        public void Evaluate_NullResults_ThrowsArgumentNullException()
        {
            Assert.That(() => FootprintPlacement.Evaluate(Line3East, AllWalkable, NothingTaken, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Evaluate_NullCells_ThrowsArgumentNullException()
        {
            Assert.That(() => FootprintPlacement.Evaluate(null, AllWalkable, NothingTaken, new List<FootprintCell>()),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---------------------------------------------------------------- null delegates

        [Test]
        public void Evaluate_NullIsWalkable_TreatsEveryCellAsWalkable()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            bool valid = FootprintPlacement.Evaluate(Line3East, null, NothingTaken, results);

            Assert.That(valid, Is.True);
            Assert.That(results.ConvertAll(r => r.Status), Has.All.EqualTo(FootprintCellStatus.Valid));
        }

        [Test]
        public void Evaluate_NullIsTakenByOther_TreatsNothingAsTaken()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            bool valid = FootprintPlacement.Evaluate(Line3East, AllWalkable, null, results);

            Assert.That(valid, Is.True);
            Assert.That(results.ConvertAll(r => r.Status), Has.All.EqualTo(FootprintCellStatus.Valid));
        }

        [Test]
        public void Evaluate_BothDelegatesNull_ReturnsTrueWithAllCellsValid()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            bool valid = FootprintPlacement.Evaluate(Line3East, null, null, results);

            Assert.That(valid, Is.True);
            Assert.That(results, Has.Count.EqualTo(3));
        }

        [Test]
        public void Evaluate_NullIsWalkableButCellTaken_ReportsOverlap()
        {
            List<FootprintCell> results = new List<FootprintCell>();

            FootprintPlacement.Evaluate(Line3East, null, InSet(Cell(3, 2)), results);

            Assert.That(StatusOf(results, Cell(3, 2)), Is.EqualTo(FootprintCellStatus.Overlap));
        }

        // ---------------------------------------------------------------- FootprintCell

        [TestCase(FootprintCellStatus.Valid, true)]
        [TestCase(FootprintCellStatus.NotWalkable, false)]
        [TestCase(FootprintCellStatus.Overlap, false)]
        public void FootprintCell_IsValid_TrueOnlyForValidStatus(FootprintCellStatus status, bool expected)
        {
            FootprintCell cell = new FootprintCell(Cell(1, 1), status);

            Assert.That(cell.IsValid, Is.EqualTo(expected));
        }
    }
}
