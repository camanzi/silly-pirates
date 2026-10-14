using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridElement : MonoBehaviour
{

    [Header("Configs")]
    [SerializeField] private LayerMask floorGridLayer;
    [SerializeField] private GridStateDataSO _gridStateData;

    [Header("Footprint")]
    [Tooltip("The cells this element covers. The anchor is the cell under the pivot and is always covered. " +
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

    // The cells actually registered, kept so unregistering never depends on computing the footprint again:
    // editing the footprint or moving the colliders in Play would otherwise leave stale occupants behind.
    private readonly List<Vector3Int> _occupiedCells = new List<Vector3Int>();

    // The footprint as cube offsets from the anchor (index 0 is the zero offset), computed once by
    // InitializePosition / RefreshFootprint. Stored relative so that the gridPosition setter can move the
    // element without probing the colliders again. Empty means anchor only.
    private readonly List<Vector3Int> _footprintOffsets = new List<Vector3Int>();

    // Scratch buffer for the collider probe. Main-thread only, like the rest of the grid code.
    private static readonly List<Vector3Int> ProbeBuffer = new List<Vector3Int>(ColliderFootprintProbe.MaxCells);

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
    /// Computes the footprint again from the current colliders and re-registers it on the current
    /// <see cref="gridPosition"/>. The footprint is only computed when the element finds its floor
    /// (<see cref="OnEnable"/>): call this after rotating the element, or enabling/disabling/resizing its
    /// colliders, at runtime. The anchor is kept, so rotate around the anchor cell. No-op before the element
    /// has found its floor.
    /// </summary>
    public void RefreshFootprint()
    {
        if (_floorTilemap == null) return;

        ComputeFootprintOffsets(_floorTilemap, _gridPosition);
        UnregisterAll();
        RegisterAt(_gridPosition);
        WarnAboutUnwalkableCells();
    }

    /// <summary>
    /// The footprint this element would have right now, without registering anything: the layout to read cells
    /// from, the anchor cell under the pivot, and the covered cells (index 0 is the anchor). Always computed
    /// live from the current transform and colliders, so it follows a rotation before any
    /// <see cref="RefreshFootprint"/>. The single entry point for previews (the editor overlay, a future
    /// layout tool), which run before or outside <see cref="OnEnable"/>.
    /// <para>In Play, once the floor is found, it uses the registered tilemap and <see cref="gridPosition"/>.
    /// Otherwise it raycasts like the runtime lookup and, when that misses (e.g. Prefab Mode, where the floor
    /// collider lives in another physics scene), falls back to the first floor-layer tilemap under the same
    /// root. The root is searched rather than the parents because the grid is not necessarily an ancestor:
    /// on the ship it is a sibling (PF_Ship/ShipGrid next to PF_Ship/Equipments).</para>
    /// </summary>
    /// <returns>False when no floor can be resolved: <paramref name="cells"/> is then left empty.</returns>
    public bool TryResolveFootprintPreview(out GridLayout layout, out Vector3Int anchor, List<Vector3Int> cells)
    {
        cells.Clear();
        if (!TryResolveAnchorPreview(out layout, out anchor)) return false;

        ComputeFootprintCells(layout, anchor, cells);
        return true;
    }

    /// <summary>
    /// Test hook: replaces the footprint with <paramref name="cubeOffsets"/> (cube offsets from the anchor, as
    /// produced by <see cref="HexFootprint.ToOffsets"/>), as if the probe had computed them. Like a probe, it
    /// does not touch the cells already registered: they change on the next <see cref="gridPosition"/> set.
    /// </summary>
    internal void SetFootprintOffsets(IReadOnlyList<Vector3Int> cubeOffsets)
    {
        _footprintOffsets.Clear();
        _footprintOffsets.Add(Vector3Int.zero);
        if (cubeOffsets == null) return;

        for (int i = 0; i < cubeOffsets.Count; i++)
            if (!_footprintOffsets.Contains(cubeOffsets[i])) _footprintOffsets.Add(cubeOffsets[i]);
    }

    private bool TryResolveAnchorPreview(out GridLayout layout, out Vector3Int anchor)
    {
        if (Application.isPlaying && _floorTilemap != null)
        {
            layout = _floorTilemap;
            anchor = _gridPosition;
            return true;
        }

        // Outside Play nothing syncs the transforms the user just moved (Auto Sync Transforms is off).
        if (!Application.isPlaying) Physics.SyncTransforms();

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

    /// <summary>Clears <paramref name="cells"/> and fills it with the footprint on <paramref name="anchor"/>, anchor first.</summary>
    private void ComputeFootprintCells(GridLayout layout, Vector3Int anchor, List<Vector3Int> cells)
    {
        if (_footprint.Source == FootprintSource.Collider)
        {
            if (!ColliderFootprintProbe.Probe(transform, layout, anchor, cells))
            {
                Debug.LogWarning(
                    $"[{nameof(GridElement)}] '{name}': footprint source is Collider but no active non-trigger " +
                    "collider was found under it. Only the anchor cell is covered.", this);
            }
            return;
        }

        cells.Clear();
        cells.Add(anchor);
    }

    private void ComputeFootprintOffsets(GridLayout layout, Vector3Int anchor)
    {
        ComputeFootprintCells(layout, anchor, ProbeBuffer);
        HexFootprint.ToOffsets(anchor, ProbeBuffer, _footprintOffsets);
        ProbeBuffer.Clear();
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
        HexFootprint.Translate(anchor, _footprintOffsets, _occupiedCells);
        for (int i = 0; i < _occupiedCells.Count; i++)
            _gridStateData.RegisterOccupancy(_occupiedCells[i], this);
    }

    private void InitializePosition()
    {
        if (TryFindFloor(out Tilemap tilemap, out Vector3Int pos))
        {
            _floorTilemap = tilemap;
            ComputeFootprintOffsets(tilemap, pos);
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
                "Check the element's colliders, rotation or placement.", this);
        }
    }

    protected virtual void OnValidate()
    {
        if (this is IMovable && _footprint.Source != FootprintSource.Single)
        {
            Debug.LogWarning(
                $"[{nameof(GridElement)}] '{name}' is movable but its footprint source is {_footprint.Source}: " +
                "movement is single-cell and does not follow rotation, only the anchor would move correctly.", this);
        }
    }
}
