using UnityEngine;

/// <summary>
/// A walkable ship tile that also marks a Broadside firing zone. Offensive equipment standing on it fires in
/// the wave of its <see cref="FiringPriority"/>: every equipment of priority 1 first, then 2, and so on (see
/// <see cref="ShipBroadsideController"/>).
///
/// A subclass rather than a field on <see cref="TerrainTile"/>: every consumer reads tiles through
/// GetTile&lt;TerrainTile&gt;, so walkability keeps working unchanged, and the other terrain tiles (walls,
/// highlight previews) do not carry a priority that would mean nothing for them.
/// </summary>
[CreateAssetMenu(fileName = "Broadside Zone Tile", menuName = "Tiles/Terrain/Broadside Zone Tile")]
public class BroadsideZoneTile : TerrainTile
{
    [Tooltip("Broadside wave of the equipment standing on this tile: lower fires first. Gaps between values are allowed")]
    [SerializeField, Min(1)] private int _firingPriority = 1;

    [Tooltip("Floating indicator spawned once per AREA of this zone (connected cells with the same priority), " +
             "not per cell. Do not use the Tile's own GameObject field for this: that one spawns per cell")]
    [SerializeField] private GameObject _indicatorPrefab;

    public int FiringPriority => _firingPriority;
    public GameObject IndicatorPrefab => _indicatorPrefab;
}
