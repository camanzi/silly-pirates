using UnityEngine;
public struct TargetingData
{
    public TargetingData(Vector3 worldPosition, Vector3Int cellPosition, bool isOverValidGrid)
    {
        this.worldPosition = worldPosition;
        this.cellPosition = cellPosition;
        this.isOverValidGrid = isOverValidGrid;
    }

    public Vector3 worldPosition;   
    public Vector3Int cellPosition;  
    public bool isOverValidGrid;

    public static TargetingData Empty => new TargetingData {};
}