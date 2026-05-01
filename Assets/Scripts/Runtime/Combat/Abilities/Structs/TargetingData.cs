using UnityEngine;
public struct TargetingData
{
    public Vector3? worldPosition;   
    public Vector3Int cellPosition;  
    public bool isOverValidGrid;
    public ITargettable selectedTarget;

    public TargetingData(IInteractableElement caster)
    {
        this.cellPosition = (caster as GridElement)?.gridPosition ?? default;
        this.worldPosition = caster.Transform.position;
        
        this.selectedTarget = null;
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