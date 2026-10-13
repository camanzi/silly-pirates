using NUnit.Framework;
using UnityEditor;

namespace SillyPirates.Tests.EditMode.TileDef
{
    /// <summary>
    /// Pins that <see cref="BroadsideZoneTile.FiringPriority"/> exposes the serialized <c>_firingPriority</c>.
    /// The field is private with no seam, so it is written through SerializedObject; FindProperty returning null
    /// on a rename turns into a loud failure instead of a silent pass.
    /// </summary>
    public class BroadsideZoneTileTests : ScriptableObjectFixture
    {
        [Test]
        public void FiringPriority_FreshInstance_IsOne()
        {
            BroadsideZoneTile tile = NewSO<BroadsideZoneTile>();

            int priority = tile.FiringPriority;

            Assert.That(priority, Is.EqualTo(1));
        }

        [Test]
        public void FiringPriority_SerializedFieldSet_ReturnsSerializedValue()
        {
            BroadsideZoneTile tile = NewSO<BroadsideZoneTile>();
            var so = new SerializedObject(tile);
            SerializedProperty prop = so.FindProperty("_firingPriority");
            Assert.That(prop, Is.Not.Null, "BroadsideZoneTile._firingPriority not found: was it renamed?");
            prop.intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            int priority = tile.FiringPriority;

            Assert.That(priority, Is.EqualTo(4));
        }
    }
}
