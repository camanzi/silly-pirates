using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Shapes
{
    /// <summary>
    /// LineShape.GetCells is an unimplemented stub: it logs and returns an empty list, which means any
    /// ability configured with ShapeType.Line silently affects nothing.
    ///
    /// This test documents that fact rather than pretending the shape works. It is green today and will go
    /// red the moment somebody implements LineShape — at which point it must be replaced by real coverage
    /// of the line geometry, not deleted.
    /// </summary>
    public class LineShapeTests
    {
        [TestCase(0, 0, 1)]
        [TestCase(0, 0, 3)]
        [TestCase(2, -3, 5)]
        public void GetCells_AnyInput_ReturnsEmptyListBecauseLineShapeIsNotImplemented(int cx, int cy, int range)
        {
            LineShape shape = new LineShape();

            List<Vector3> cells = shape.GetCells(new Vector3Int(cx, cy, 0), range, Vector3Int.zero);

            Assert.That(cells, Is.Not.Null);
            Assert.That(cells, Is.Empty);
        }
    }
}
