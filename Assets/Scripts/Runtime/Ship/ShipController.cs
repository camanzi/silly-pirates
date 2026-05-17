using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ShipController : MonoBehaviour
{

    [Header("Map Tags")]
    [SerializeField] private string _modelsMapTag;
    [SerializeField] private string _floorMapTag;
    [SerializeField] private string _previewMapTag;
    [SerializeField] private string _persistentEffectsMapTag;

    [Header("Tiles")]
    [SerializeField] private Tile _defaultFloorTile;

    private Grid _shipGrid;
    private Tilemap _modelsMap;
    private Tilemap _floorMap;
    private Tilemap _previewMap;
    private Tilemap _persistentEffectsMap;

    private Dictionary<string, (TilemapTarget target, List<Vector3Int> cells)> _activeLayers = new();

    private void Awake()
    {
        _shipGrid = GetComponentInChildren<Grid>();

        _modelsMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _modelsMapTag);
        _floorMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _floorMapTag);
        _previewMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _previewMapTag);
        _persistentEffectsMap = FindChildByTag<Tilemap>(_shipGrid.gameObject, _persistentEffectsMapTag);
    }

    private T FindChildByTag<T>(GameObject parent, string tag)
    {
        T result = default(T);

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.tag.Equals(tag))
            {
                result = child.GetComponent<T>();
            }
        }

        return result;
    }

    public void HighlightPath(HighlightGridPayload payload)
    {
        if (payload.Layers == null) return;
        foreach (var layer in payload.Layers)
            ApplyLayer(layer);
    }

    private void ApplyLayer(CellOverlayLayer layer)
    {
        if (_activeLayers.TryGetValue(layer.Key, out var prev))
        {
            Tilemap prevMap = prev.target == TilemapTarget.Preview ? _previewMap : _persistentEffectsMap;
            foreach (var pos in prev.cells)
                prevMap.SetTile(pos, null);
        }

        if (layer.Cells == null || layer.Cells.Count == 0)
        {
            _activeLayers.Remove(layer.Key);
            return;
        }

        Tilemap map = layer.Target == TilemapTarget.Preview ? _previewMap : _persistentEffectsMap;
        _activeLayers[layer.Key] = (layer.Target, new List<Vector3Int>(layer.Cells));
        foreach (var pos in layer.Cells)
            map.SetTile(pos, layer.Tile);
    }
}
