using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable
{
    
    public override void ExecuteAction(Vector3Int targetPosition)
    {
        base.ExecuteAction(targetPosition);
        MoveTo(targetPosition);
    }
    public void MoveTo(Vector3Int position)
    {
        _gridPosition = position;
        transform.position = _floorTilemap.CellToWorld(position);
        Debug.Log($"Mi sono mosso nella posizione selezionata! {position}");
    }
}
