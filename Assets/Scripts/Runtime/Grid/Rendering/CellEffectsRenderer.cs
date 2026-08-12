using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CellEffectsRenderer : MonoBehaviour
{
    [SerializeField] private Grid _targetGrid;
    [SerializeField] private Tilemap _templateTilemap;
    [SerializeField] private TileBase _effectTile;
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _baseSortingOrder = 5;

    private readonly Dictionary<string, Tilemap> _tilemapByKey = new();
    private readonly Dictionary<string, List<Vector3Int>> _cellsByKey = new();
    private int _sortingCounter;

    public void OnEffectReceived(CellEffectPayload payload)
    {
        bool isClearing = payload.Cells == null || payload.Cells.Count == 0;

        if (isClearing)
        {
            if (_tilemapByKey.TryGetValue(payload.Key, out Tilemap existing))
            {
                Destroy(existing.gameObject);
                _tilemapByKey.Remove(payload.Key);
            }
            _cellsByKey.Remove(payload.Key);
            return;
        }

        if (!_tilemapByKey.TryGetValue(payload.Key, out Tilemap tilemap))
            tilemap = CreateTilemap(payload.Key, payload.Material);

        _cellsByKey.TryGetValue(payload.Key, out List<Vector3Int> oldCells);

        if (oldCells != null)
        {
            foreach (var cell in oldCells)
                if (!payload.Cells.Contains(cell))
                    tilemap.SetTile(cell, null);
        }

        foreach (var cell in payload.Cells)
            tilemap.SetTile(cell, _effectTile);

        _cellsByKey[payload.Key] = new List<Vector3Int>(payload.Cells);
    }

    private Tilemap CreateTilemap(string key, Material material)
    {
        var go = new GameObject($"CellEffect_{key}");
        go.transform.SetParent(_targetGrid.transform, false);

        var tilemap = go.AddComponent<Tilemap>();
        if (_templateTilemap != null)
        {
            tilemap.orientation = _templateTilemap.orientation;
            tilemap.tileAnchor  = _templateTilemap.tileAnchor;
        }
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = _sortingLayerName;
        renderer.sortingOrder = _baseSortingOrder + _sortingCounter++;
        // sharedMaterial e non `new Material(...)`: qui non si imposta nessuna proprieta' per-istanza,
        // e una Material creata a mano non verrebbe distrutta con il GameObject (leak a ogni ciclo crea/cancella).
        renderer.sharedMaterial = material;

        _tilemapByKey[key] = tilemap;
        return tilemap;
    }
}
