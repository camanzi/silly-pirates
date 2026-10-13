using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Places one floating indicator above every Broadside AREA of the ship (connected cells of the same
/// <see cref="BroadsideZoneTile"/> priority, see <see cref="BroadsideZoneAreas"/>), at the area's centroid plus
/// a height. The prefab comes from the zone tile itself.
///
/// When: once, when the combat begins — never during the intro cutscene. OnCombatStarted is raised only at the
/// end of a full intro (not when it is skipped), so the same gate as WorldSpaceCharacterStatusController is
/// used: no intro active at Start means spawn at once, otherwise wait for the event. Hidden (SetActive false)
/// when the combat is resolved.
///
/// The ship floor is never rewritten at runtime, so the areas are computed once. The indicators are children
/// of the ship in the combat scene and die with it: no state to reset between combats.
/// Lives on the ship root next to <see cref="ShipController"/>.
/// </summary>
[RequireComponent(typeof(ShipController))]
public class BroadsideZoneIndicatorSpawner : MonoBehaviour
{
    [Header("Combat lifecycle")]
    [Tooltip("When the intro is active the indicators wait for the combat start; otherwise they appear at once")]
    [SerializeField] private CombatIntroStateSO _introState;
    [Tooltip("Raised at the end of the intro sequence: the moment the indicators appear")]
    [SerializeField] private VoidEventChannel _onCombatStarted;
    [Tooltip("The indicators are hidden when the combat is resolved")]
    [SerializeField] private CombatOutcomeStateSO _outcomeState;

    [Header("Placement")]
    [Tooltip("Height of the indicator above the centroid of its area, in world units")]
    [SerializeField] [Min(0f)] private float _heightAboveDeck = 2.5f;

    private ShipController _ship;
    private bool _spawned;
    private bool _combatOver;
    private readonly List<GameObject> _indicators = new();

    private void Awake() => _ship = GetComponent<ShipController>();

    private void OnEnable()
    {
        if (_onCombatStarted != null) _onCombatStarted.OnEventRaised += HandleCombatStarted;

        if (_outcomeState != null)
        {
            // Read-then-subscribe: an outcome resolved before this enables would otherwise be missed.
            if (_outcomeState.IsCombatOver) HandleCombatResolved(_outcomeState.Outcome);
            _outcomeState.OnCombatResolved += HandleCombatResolved;
        }
    }

    private void OnDisable()
    {
        if (_onCombatStarted != null) _onCombatStarted.OnEventRaised -= HandleCombatStarted;
        if (_outcomeState != null) _outcomeState.OnCombatResolved -= HandleCombatResolved;
    }

    // Start, not OnEnable: the intro sequencer raises IsIntroActive in its Awake, and the floor map is resolved
    // in ShipController.Awake. Both are guaranteed to have run by now.
    private void Start()
    {
        if (_introState == null || !_introState.IsIntroActive)
            SpawnIndicators();
    }

    private void HandleCombatStarted() => SpawnIndicators();

    private void HandleCombatResolved(CombatOutcome outcome)
    {
        _combatOver = true;
        for (int i = 0; i < _indicators.Count; i++)
            if (_indicators[i] != null) _indicators[i].SetActive(false);
    }

    private void SpawnIndicators()
    {
        if (_spawned || _combatOver) return;
        _spawned = true;

        Tilemap floor = _ship != null ? _ship.FloorMap : null;
        if (floor == null)
        {
            Debug.LogWarning($"[{nameof(BroadsideZoneIndicatorSpawner)}] No floor tilemap on the ship: no Broadside indicators.", this);
            return;
        }

        var zoneCells = new List<Vector3Int>();
        foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
            if (floor.GetTile<BroadsideZoneTile>(cell) != null) zoneCells.Add(cell);

        if (zoneCells.Count == 0) return;

        var areas = new List<BroadsideZoneArea>();
        BroadsideZoneAreas.FindAreas(zoneCells,
            cell => floor.GetTile<BroadsideZoneTile>(cell)?.FiringPriority,
            areas);

        for (int i = 0; i < areas.Count; i++)
        {
            BroadsideZoneArea area = areas[i];
            BroadsideZoneTile tile = floor.GetTile<BroadsideZoneTile>(area.Cells[0]);
            if (tile.IndicatorPrefab == null)
            {
                Debug.LogWarning($"[{nameof(BroadsideZoneIndicatorSpawner)}] '{tile.name}' has no indicator prefab: " +
                                 $"the priority-{area.Priority} area at {area.Cells[0]} gets no indicator.", tile);
                continue;
            }

            // Cell centres from the tilemap: the ship grid is hexagonal and swizzled, never compute them by hand.
            Vector3 position = BroadsideZoneAreas.Centroid(area.Cells, floor.GetCellCenterWorld)
                               + Vector3.up * _heightAboveDeck;
            _indicators.Add(Instantiate(tile.IndicatorPrefab, position, Quaternion.identity, transform));
        }
    }
}
