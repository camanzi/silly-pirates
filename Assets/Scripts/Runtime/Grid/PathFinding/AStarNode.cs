using UnityEngine;

public struct AStarNode
{
    public Vector3Int position;
    public bool isWalkable;
    public int gCost;
    public int hCost;
    public Vector3Int? parentPosition;

    public int fCost => gCost + hCost; 

    public AStarNode(Vector3Int position, bool isWalkable, int gCost, int hCost, Vector3Int? parentPosition) {
        this.position = position;
        this.isWalkable = isWalkable;
        this.gCost = gCost;
        this.hCost = hCost;
        this.parentPosition = parentPosition;
    }
}
