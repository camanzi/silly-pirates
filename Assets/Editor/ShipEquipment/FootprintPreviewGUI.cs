using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// IMGUI mini-map of a <see cref="GridFootprint"/>: an idealised pointy-top hex grid around the anchor, drawn
/// from <see cref="HexCellGeometry"/>'s planar geometry, so it needs no scene and works in Prefab Mode.
/// Pure view: it reports clicks, the caller decides what to write.
/// </summary>
public static class FootprintPreviewGUI
{
    /// <summary>Rings of cells drawn around the anchor. A Line of 9 reaches exactly 4 cells out.</summary>
    public const int GridRadius = 4;

    // Colours shared with the Scene view overlay so both views read the same.
    public static readonly Color ValidColor = new Color(0.2f, 0.85f, 1f, 1f);
    public static readonly Color AnchorColor = new Color(1f, 0.55f, 0f, 1f);
    public static readonly Color NotWalkableColor = new Color(1f, 0.2f, 0.2f, 1f);
    public static readonly Color OverlapColor = new Color(1f, 0.25f, 0.9f, 1f);
    public const float FillAlpha = 0.35f;

    private static readonly Color BackgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ClickableLineColor = new Color(1f, 1f, 1f, 0.35f);

    private static readonly List<Vector3Int> FootprintCells = new List<Vector3Int>(HexFootprint.MaxLineCount);
    private static readonly Vector2[] PlanarCorners = new Vector2[HexCellGeometry.CornerCount];
    private static readonly Vector3[] Polygon = new Vector3[HexCellGeometry.CornerCount];
    private static readonly Vector3[] Outline = new Vector3[HexCellGeometry.CornerCount + 1];

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
    /// Draws the mini-map inside <paramref name="rect"/>. <paramref name="sceneStatus"/> is the last placement
    /// evaluation from the scene (or null): it is matched by index, which holds as long as it was computed for
    /// the same footprint, since both come from <see cref="HexFootprint.Resolve(Vector3Int, GridFootprint, List{Vector3Int})"/>.
    /// Returns true when the user clicked one of the anchor's neighbours, with the matching direction.
    /// </summary>
    public static bool Draw(Rect rect, GridFootprint footprint, IReadOnlyList<FootprintCell> sceneStatus,
                            out HexDirection clickedDirection)
    {
        clickedDirection = footprint.Direction;

        // Fit 2R+1 hexes across (sqrt3 * size each) and 1.5 * 2R + 2 hex radii down.
        int across = GridRadius * 2 + 1;
        float size = Mathf.Min(rect.width / (Mathf.Sqrt(3f) * across), rect.height / (3f * GridRadius + 2f));
        size *= 0.95f;
        Vector2 origin = rect.center;
        bool canPickDirection = footprint.Shape != FootprintShape.Single;

        Event evt = Event.current;
        if (canPickDirection && evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        {
            for (int i = 0; i < HexCoordinates.DirectionCount; i++)
            {
                Vector3Int neighbour = HexCoordinates.Neighbour(Vector3Int.zero, (HexDirection)i);
                if (Vector2.Distance(evt.mousePosition, ToGui(origin, neighbour, size)) > size * 0.85f) continue;
                clickedDirection = (HexDirection)i;
                evt.Use();
                return true;
            }
        }

        if (evt.type != EventType.Repaint) return false;

        EditorGUI.DrawRect(rect, BackgroundColor);
        GUI.BeginClip(rect);
        // Inside the clip, (0,0) is the rect's top-left corner.
        origin = rect.size * 0.5f;

        // Background grid: every cell within GridRadius of the anchor (0,0).
        for (int q = -GridRadius; q <= GridRadius; q++)
        {
            for (int r = -GridRadius; r <= GridRadius; r++)
            {
                if (Mathf.Abs(q + r) > GridRadius) continue;
                Vector3Int cell = HexCoordinates.CubeToOffset(new Vector3Int(q, r, -q - r));
                bool clickable = canPickDirection
                              && HexCoordinates.CubeDistance(Vector3Int.zero, new Vector3Int(q, r, -q - r)) == 1;
                BuildHex(origin, cell, size);
                Handles.color = clickable ? ClickableLineColor : GridLineColor;
                Handles.DrawAAPolyLine(1.5f, Outline);
            }
        }

        // Footprint cells.
        HexFootprint.Resolve(Vector3Int.zero, footprint, FootprintCells);
        bool statusMatches = sceneStatus != null && sceneStatus.Count == FootprintCells.Count;
        for (int i = 0; i < FootprintCells.Count; i++)
        {
            FootprintCellStatus status = statusMatches ? sceneStatus[i].Status : FootprintCellStatus.Valid;
            Color color = StatusColor(status, i == 0);
            BuildHex(origin, FootprintCells[i], size);
            Handles.color = new Color(color.r, color.g, color.b, FillAlpha);
            Handles.DrawAAConvexPolygon(Polygon);
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, Outline);
        }

        // Anchor marker, kept visible even when the anchor cell itself is invalid.
        Handles.color = AnchorColor;
        Handles.DrawSolidDisc(ToGui(origin, Vector3Int.zero, size), Vector3.forward, size * 0.18f);

        if (footprint.HasDirection)
        {
            Vector2 from = ToGui(origin, Vector3Int.zero, size);
            Vector2 to = ToGui(origin, HexCoordinates.Neighbour(Vector3Int.zero, footprint.Direction), size);
            DrawArrow(from, to);
        }

        GUI.EndClip();
        return false;
    }

    private static void DrawArrow(Vector2 from, Vector2 to)
    {
        Vector2 back = (from - to) * 0.3f;
        Vector2 side = new Vector2(-back.y, back.x) * 0.5f;
        Handles.color = AnchorColor;
        Handles.DrawAAPolyLine(3f, from, to);
        Handles.DrawAAPolyLine(3f, to + back + side, to, to + back - side);
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
