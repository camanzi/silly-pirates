using UnityEngine;
public struct TargetingData
{
    public Vector3? worldPosition;   
    public Vector3Int cellPosition;  
    public bool isOverValidGrid;
    public ITargettable selectedTarget;

    public TargetingData(Vector3Int cellPosition)
    {
        this.cellPosition = cellPosition;
        this.selectedTarget = null;
        this.worldPosition = null;
        this.isOverValidGrid = false;
    }

    public TargetingData(Vector3 worldPosition, Vector3Int cellPosition, bool isOverValidGrid)
    {
        this.worldPosition = worldPosition;
        this.cellPosition = cellPosition;
        this.isOverValidGrid = isOverValidGrid;
        this.selectedTarget = null;
    }

    public TargetingData(Vector3 worldPosition, Vector3Int cellPosition, bool isOverValidGrid, ITargettable selectedTarget)
    {
        this.worldPosition = worldPosition;
        this.cellPosition = cellPosition;
        this.isOverValidGrid = isOverValidGrid;
        this.selectedTarget = selectedTarget;
    }

    public static TargetingData Empty => new TargetingData {};
}