using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Shapes
{
    /// <summary>
    /// ShapeFactory serves shapes out of a static readonly dictionary, so every ability shares one
    /// instance per ShapeType. That sharing is the reason CircleShape must stay stateless.
    /// </summary>
    public class ShapeFactoryTests
    {
        [Test]
        public void GetShape_CircleType_ReturnsCircleShape()
        {
            IAreaShape shape = ShapeFactory.GetShape(ShapeType.Circle);

            Assert.That(shape, Is.TypeOf<CircleShape>());
        }

        [Test]
        public void GetShape_LineType_ReturnsLineShape()
        {
            IAreaShape shape = ShapeFactory.GetShape(ShapeType.Line);

            Assert.That(shape, Is.TypeOf<LineShape>());
        }

        /// <summary>
        /// Pins the shared-singleton design explicitly: whoever changes this to return a new instance per
        /// call changes the memory profile of every ability preview, and should do it deliberately.
        /// </summary>
        [TestCase(ShapeType.Circle)]
        [TestCase(ShapeType.Line)]
        public void GetShape_SameTypeTwice_ReturnsTheSameInstance(ShapeType type)
        {
            IAreaShape first = ShapeFactory.GetShape(type);
            IAreaShape second = ShapeFactory.GetShape(type);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetShape_UnknownShapeType_ReturnsNull()
        {
            IAreaShape shape = ShapeFactory.GetShape((ShapeType)999);

            Assert.That(shape, Is.Null);
        }
    }
}
