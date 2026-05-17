using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TilemapTarget { Preview, PersistentEffects }

public struct CellOverlayLayer
{
    public string Key;
    public List<Vector3Int> Cells; // null or empty = clear this layer
    public Tile Tile;              // ignored when clearing
    public TilemapTarget Target;
}

public static class HighlightLayerKeys
{
    public const string PreviewAffected    = "preview-affected";
    public const string PreviewInteraction = "preview-interaction";
    public const string OceanCurrents     = "ocean-currents";
    public const string StarPath          = "star-path";
}

public struct HighlightGridPayload
{
    public List<CellOverlayLayer> Layers; // null = no-op
}
