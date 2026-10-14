using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SillyPirates.Tests.EditMode.Grid.Utils
{
    /// <summary>
    /// <see cref="GridUtils.IsWalkable"/>: the single walkability rule shared by the footprint preview and the
    /// placement validation. Runs against a real Tilemap, which needs no scene to hold tiles.
    /// </summary>
    public class GridUtilsTests : ScriptableObjectFixture
    {
        private Tilemap _tilemap;

        [SetUp]
        public void SetUp()
        {
            GameObject gridGo = Track(new GameObject("Grid", typeof(UnityEngine.Grid)));
            GameObject tilemapGo = Track(new GameObject("Floor", typeof(Tilemap)));
            tilemapGo.transform.SetParent(gridGo.transform);
            _tilemap = tilemapGo.GetComponent<Tilemap>();
        }

        private TerrainTile NewTerrainTile(bool walkable)
        {
            TerrainTile tile = NewSO<TerrainTile>();
            using var so = new SerializedObject(tile);
            so.FindProperty("_isWalkable").boolValue = walkable;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        [Test]
        public void IsWalkable_WalkableTerrainTile_ReturnsTrue()
        {
            var cell = new Vector3Int(2, 3, 0);
            _tilemap.SetTile(cell, NewTerrainTile(walkable: true));

            Assert.That(GridUtils.IsWalkable(_tilemap, cell), Is.True);
        }

        [Test]
        public void IsWalkable_NotWalkableTerrainTile_ReturnsFalse()
        {
            var cell = new Vector3Int(2, 3, 0);
            _tilemap.SetTile(cell, NewTerrainTile(walkable: false));

            Assert.That(GridUtils.IsWalkable(_tilemap, cell), Is.False);
        }

        [Test]
        public void IsWalkable_EmptyCell_ReturnsFalse()
        {
            Assert.That(GridUtils.IsWalkable(_tilemap, new Vector3Int(-4, 1, 0)), Is.False);
        }

        [Test]
        public void IsWalkable_PlainTileThatIsNotATerrainTile_ReturnsFalse()
        {
            var cell = new Vector3Int(0, -2, 0);
            _tilemap.SetTile(cell, NewSO<Tile>());

            Assert.That(GridUtils.IsWalkable(_tilemap, cell), Is.False);
        }

        [Test]
        public void IsWalkable_SubclassOfTerrainTile_UsesItsFlag()
        {
            // BroadsideZoneTile extends TerrainTile: the rule must accept subclasses, not only the exact type.
            var cell = new Vector3Int(1, 1, 0);
            BroadsideZoneTile tile = NewSO<BroadsideZoneTile>();
            using (var so = new SerializedObject(tile))
            {
                so.FindProperty("_isWalkable").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            _tilemap.SetTile(cell, tile);

            Assert.That(GridUtils.IsWalkable(_tilemap, cell), Is.True);
        }
    }
}
