using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Terrain Tile", menuName ="Tiles/Terrain/Terrain Tile")]
public class TerrainTile : Tile
{
    public bool isWalkable => _isWalkable;
    
    [SerializeField] private bool _isWalkable;
}
