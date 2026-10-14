using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridElement : MonoBehaviour
{

    [Header("Configs")]
    [SerializeField] private LayerMask floorGridLayer;
    [SerializeField] private GridStateDataSO _gridStateData;

    [Header("Footprint")]
    [Tooltip("The cells this element covers. The pivot sits on the anchor cell (the centre of the shape). " +
             "Movable characters must stay Single: movement is single-cell.")]
    [SerializeField] private GridFootprint _footprint = GridFootprint.Single;

    public Vector3Int gridPosition {
        get { return _gridPosition; }
        set
        {
            UnregisterAll();
            OnGridPositionChanged(_gridPosition, value);
            _gridPosition = value;
            RegisterAt(_gridPosition);
        }
    }

    protected virtual void OnGridPositionChanged(Vector3Int prevPosition, Vector3Int newPosition) { }

    public Vector3 worldPosition => _floorTilemap.CellToWorld(gridPosition);
    public Tilemap activeTilemap =>_floorTilemap;
    protected Tilemap _floorTilemap;

    public GridFootprint Footprint => _footprint;

    /// <summary>
    /// The cells currently registered in <see cref="GridStateDataSO"/> for this element. Index 0 is the anchor
    /// (<see cref="gridPosition"/>). Empty until the element has found its floor.
    /// </summary>
    public IReadOnlyList<Vector3Int> OccupiedCells => _occupiedCells;

    private Vector3Int _gridPosition;

    // The cells actually registered, kept so unregistering never depends on resolving the footprint again:
    // editing the footprint in Play would otherwise leave stale occupants behind.
    private readonly List<Vector3Int> _occupiedCells = new List<Vector3Int>();

    protected virtual void Awake() { }

    protected virtual void OnEnable()
    {
        InitializePosition();
    }

    protected virtual void OnDisable()
    {
        // Without this, unloading the scene leaves the destroyed GridElement inside
        // GridStateDataSO._occupiedCells: IsOccupied() is a dictionary lookup, not a Unity null check, so
        // in the next combat that cell reads as occupied forever and pathfinding dies. Subclasses that
        // override OnDisable MUST call base.OnDisable() so they do not silence this unregister.
        UnregisterAll();
    }

    /// <summary>True when <paramref name="cell"/> is one of the cells this element is registered on.</summary>
    public bool Occupies(Vector3Int cell) => _occupiedCells.Contains(cell);

    /// <summary>
    /// Distance in cells (like <see cref="PathFindingUtils.GetNormalizedDistance"/>) from the closest cell of
    /// the footprint to <paramref name="cell"/>. Falls back to <see cref="gridPosition"/> when nothing is
    /// registered yet.
    /// </summary>
    public int DistanceTo(Vector3Int cell)
    {
        if (_occupiedCells.Count == 0)
            return PathFindingUtils.GetNormalizedDistance(_gridPosition, cell);
        return HexFootprint.DistanceTo(_occupiedCells, cell);
    }

    /// <summary>
    /// Where this element would sit on the grid right now, without registering anything: the layout to
    /// read cells from and the anchor cell under the pivot. Meant for previews (the editor overlay, a future
    /// layout tool), which run before or outside <see cref="OnEnable"/>.
    /// <para>In Play, once the floor is found, it returns the registered tilemap and <see cref="gridPosition"/>.
    /// Otherwise it raycasts like the runtime lookup and, when that misses (e.g. Prefab Mode, where the floor
    /// collider lives in another physics scene), falls back to the first floor-layer tilemap under the same
    /// root. The root is searched rather than the parents because the grid is not necessarily an ancestor:
    /// on the ship it is a sibling (PF_Ship/ShipGrid next to PF_Ship/Equipments).</para>
    /// </summary>
    public bool TryResolveFloorPreview(out GridLayout layout, out Vector3Int anchor)
    {
        if (Application.isPlaying && _floorTilemap != null)
        {
            layout = _floorTilemap;
            anchor = _gridPosition;
            return true;
        }

        if (TryFindFloor(out Tilemap tilemap, out anchor))
        {
            layout = tilemap;
            return true;
        }

        layout = null;
        foreach (Tilemap candidate in transform.root.GetComponentsInChildren<Tilemap>(true))
        {
            if ((floorGridLayer.value & (1 << candidate.gameObject.layer)) == 0) continue;
            layout = candidate;
            break;
        }

        if (layout == null)
        {
            anchor = default;
            return false;
        }

        anchor = layout.WorldToCell(transform.position);
        // Same reasoning as GridInputHandler: the floor is single-layer at z=0, and with the YZX swizzle a
        // pivot above the deck would otherwise produce a Z matching no real cell.
        anchor.z = 0;
        return true;
    }

    private void UnregisterAll()
    {
        if (_gridStateData != null)
        {
            for (int i = 0; i < _occupiedCells.Count; i++)
                _gridStateData.UnregisterOccupancy(_occupiedCells[i], this);
        }
        _occupiedCells.Clear();
    }

    private void RegisterAt(Vector3Int anchor)
    {
        HexFootprint.Resolve(anchor, _footprint, _occupiedCells);
        for (int i = 0; i < _occupiedCells.Count; i++)
            _gridStateData.RegisterOccupancy(_occupiedCells[i], this);
    }

    private void InitializePosition()
    {
        if (TryFindFloor(out Tilemap tilemap, out Vector3Int pos))
        {
            _floorTilemap = tilemap;
            // Set directly to avoid triggering OnGridPositionChanged before the scene is ready
            UnregisterAll();
            _gridPosition = pos;
            RegisterAt(_gridPosition);
            WarnAboutUnwalkableCells();
        }
        else
        {
            // This is never an intended condition: with no tilemap the element registers no occupancy
            // (pathfinding treats its cell as free) and every ability reading activeTilemap blows up with
            // a NullReference much later, far away from the real cause.
            Debug.LogError(
                $"[{nameof(GridElement)}] '{name}': no floor found below {transform.position} " +
                $"(layer '{floorGridLayer.value}'). The element is left with no tilemap and no occupancy.", this);
        }
    }

    /// <summary>Raycasts down from the pivot onto the floor layer: the tilemap hit and the anchor cell under it.</summary>
    private bool TryFindFloor(out Tilemap tilemap, out Vector3Int cell)
    {
        Ray ray = new Ray(transform.position + (transform.up * 0.5f), -transform.up);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 5f, floorGridLayer)
            && hitInfo.transform.TryGetComponent(out tilemap))
        {
            cell = tilemap.WorldToCell(hitInfo.point);
            return true;
        }

        tilemap = null;
        cell = default;
        return false;
    }

    private void WarnAboutUnwalkableCells()
    {
        // Only the extra footprint cells are checked: the anchor is where the element was placed on purpose,
        // and single-cell elements (props, characters) keep behaving as before.
        for (int i = 1; i < _occupiedCells.Count; i++)
        {
            if (GridUtils.IsWalkable(_floorTilemap, _occupiedCells[i])) continue;
            Debug.LogWarning(
                $"[{nameof(GridElement)}] '{name}': footprint cell {_occupiedCells[i]} is not walkable. " +
                "Check the footprint shape/direction or the element's placement.", this);
        }
    }

    protected virtual void OnValidate()
    {
        _footprint = _footprint.Clamped();

        if (this is IMovable && _footprint.Shape != FootprintShape.Single)
        {
            Debug.LogWarning(
                $"[{nameof(GridElement)}] '{name}' is movable but its footprint is {_footprint.Shape}: movement " +
                "is single-cell, only the anchor would move correctly.", this);
        }
    }
}
