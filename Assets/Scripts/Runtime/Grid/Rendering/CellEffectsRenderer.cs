using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CellEffectsRenderer : MonoBehaviour
{
    [SerializeField] private Grid _targetGrid;
    [SerializeField] private Material _effectMaterialTemplate;
    [SerializeField] private TileBase _effectTile;
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _baseSortingOrder = 5;

    private readonly Dictionary<string, Tilemap> _tilemapByKey = new();
    private readonly Dictionary<string, List<Vector3Int>> _cellsByKey = new();
    private int _sortingCounter;

    private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int s_Color   = Shader.PropertyToID("_Color");
    private static readonly int s_Mode    = Shader.PropertyToID("_Mode");
    private static readonly int s_Params  = Shader.PropertyToID("_Params");

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
            tilemap = CreateTilemap(payload.Key, payload.Descriptor);
        else
            ApplyDescriptorToMaterial(tilemap.GetComponent<TilemapRenderer>().sharedMaterial, payload.Descriptor);

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

    private Tilemap CreateTilemap(string key, CellEffectDescriptor descriptor)
    {
        var go = new GameObject($"CellEffect_{key}");
        go.transform.SetParent(_targetGrid.transform, false);

        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = _sortingLayerName;
        renderer.sortingOrder = _baseSortingOrder + _sortingCounter++;

        var mat = new Material(_effectMaterialTemplate);
        renderer.sharedMaterial = mat;
        ApplyDescriptorToMaterial(mat, descriptor);

        _tilemapByKey[key] = tilemap;
        return tilemap;
    }

    private void ApplyDescriptorToMaterial(Material mat, CellEffectDescriptor descriptor)
    {
        mat.SetTexture(s_MainTex, descriptor.Texture != null ? descriptor.Texture : Texture2D.whiteTexture);
        mat.SetColor(s_Color, descriptor.Color);
        mat.SetFloat(s_Mode, (float)descriptor.Mode);
        mat.SetVector(s_Params, descriptor.Params);
    }
}
