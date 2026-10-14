using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// IMGUI mini-map of a footprint: an idealised pointy-top hex grid centred on the anchor, drawn from
/// <see cref="HexCellGeometry"/>'s planar geometry. The cells come from the scene (they follow the colliders),
/// and are placed relative to the anchor through cube coordinates, so the row parity of the anchor does not
/// distort the shape. Pure view: no interaction, the transform is edited from the Scene view.
/// </summary>
public static class FootprintPreviewGUI
{
    /// <summary>Fewest rings of cells drawn around the anchor; the map grows to fit larger footprints.</summary>
    public const int MinGridRadius = 2;

    /// <summary>Most rings drawn: cells further out than this are left off the map.</summary>
    public const int MaxGridRadius = 8;

    // Colours shared with the Scene view overlay so both views read the same.
    public static readonly Color ValidColor = new Color(0.2f, 0.85f, 1f, 1f);
    public static readonly Color AnchorColor = new Color(1f, 0.55f, 0f, 1f);
    public static readonly Color NotWalkableColor = new Color(1f, 0.2f, 0.2f, 1f);
    public static readonly Color OverlapColor = new Color(1f, 0.25f, 0.9f, 1f);
    public const float FillAlpha = 0.35f;

    private static readonly Color BackgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.12f);

    private static readonly Vector2[] PlanarCorners = new Vector2[HexCellGeometry.CornerCount];
    private static readonly Vector3[] Polygon = new Vector3[HexCellGeometry.CornerCount];
    private static readonly Vector3[] Outline = new Vector3[HexCellGeometry.CornerCount + 1];

    private static GUIStyle _messageStyle;

    /// <summary>The fill colour for a cell, given its status and whether it is the anchor.</summary>
    public static Color StatusColor(FootprintCellStatus status, bool isAnchor)
    {
        switch (status)
        {
            case FootprintCellStatus.NotWalkable: return NotWalkableColor;
            case FootprintCellStatus.Overlap: return OverlapColor;
            default: return isAnchor ? AnchorColor : ValidColor;
        }
    }

    /// <summary>
    /// Draws the mini-map inside <paramref name="rect"/>. <paramref name="cells"/> is the footprint with its
    /// placement status (index 0 is the anchor), or null when no floor could be resolved, in which case only a
    /// message is drawn. <paramref name="anchor"/> is the anchor cell the footprint was computed on.
    /// </summary>
    public static void Draw(Rect rect, IReadOnlyList<FootprintCell> cells, Vector3Int anchor)
    {
        if (Event.current.type != EventType.Repaint) return;

        EditorGUI.DrawRect(rect, BackgroundColor);

        if (cells == null)
        {
            _messageStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(rect, "No floor: cannot resolve the footprint.", _messageStyle);
            return;
        }

        Vector3Int anchorCube = HexCoordinates.OffsetToCube(anchor);
        int radius = MinGridRadius;
        for (int i = 0; i < cells.Count; i++)
        {
            int d = HexCoordinates.CubeDistance(HexCoordinates.OffsetToCube(cells[i].Cell), anchorCube);
            radius = Mathf.Max(radius, d + 1);
        }
        radius = Mathf.Min(radius, MaxGridRadius);

        // Fit 2R+1 hexes across (sqrt3 * size each) and 1.5 * 2R + 2 hex radii down.
        int across = radius * 2 + 1;
        float size = Mathf.Min(rect.width / (Mathf.Sqrt(3f) * across), rect.height / (3f * radius + 2f));
        size *= 0.95f;

        GUI.BeginClip(rect);
        // Inside the clip, (0,0) is the rect's top-left corner.
        Vector2 origin = rect.size * 0.5f;

        // Background grid: every cell within the radius of the anchor (0,0).
        Handles.color = GridLineColor;
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                if (Mathf.Abs(q + r) > radius) continue;
                BuildHex(origin, HexCoordinates.CubeToOffset(new Vector3Int(q, r, -q - r)), size);
                Handles.DrawAAPolyLine(1.5f, Outline);
            }
        }

        // Footprint cells, placed by their cube offset from the anchor.
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int offset = HexCoordinates.OffsetToCube(cells[i].Cell) - anchorCube;
            if (HexCoordinates.CubeDistance(offset, Vector3Int.zero) > radius) continue;

            Color color = StatusColor(cells[i].Status, i == 0);
            BuildHex(origin, HexCoordinates.CubeToOffset(offset), size);
            Handles.color = new Color(color.r, color.g, color.b, FillAlpha);
            Handles.DrawAAConvexPolygon(Polygon);
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, Outline);
        }

        // Anchor marker, kept visible even when the anchor cell itself is invalid.
        Handles.color = AnchorColor;
        Handles.DrawSolidDisc(ToGui(origin, Vector3Int.zero, size), Vector3.forward, size * 0.18f);

        GUI.EndClip();
    }

    // Planar +y is North, GUI +y is down: flip it.
    private static Vector2 ToGui(Vector2 origin, Vector3Int cell, float size)
    {
        Vector2 planar = HexCellGeometry.CellCenterPlanar(cell, size);
        return origin + new Vector2(planar.x, -planar.y);
    }

    private static void BuildHex(Vector2 origin, Vector3Int cell, float size)
    {
        Vector2 centre = ToGui(origin, cell, size);
        HexCellGeometry.GetCornersPlanar(centre, size, PlanarCorners);
        for (int i = 0; i < HexCellGeometry.CornerCount; i++)
        {
            // GetCornersPlanar works with +y up around the (already flipped) centre: mirror each corner's
            // offset too. A regular hex is symmetric, so this only changes the winding order.
            Vector2 c = PlanarCorners[i];
            Vector3 p = new Vector3(c.x, centre.y - (c.y - centre.y), 0f);
            Polygon[i] = p;
            Outline[i] = p;
        }
        Outline[HexCellGeometry.CornerCount] = Outline[0];
    }
}
