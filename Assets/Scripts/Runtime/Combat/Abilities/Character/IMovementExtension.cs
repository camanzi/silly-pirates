using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

public interface IMovementExtension
{
    IEnumerable<Vector3Int> GetExtensionCells(HashSet<Vector3Int> reachableSet, Tilemap tilemap);
    bool ClaimsTarget(Vector3Int cell);
    AbilityPreviewData GetPreview(IInteractableElement caster, TargetingData td, ref object extCache);
    bool CanExecuteOn(IInteractableElement caster, Vector3Int td, ref object extCache);
    ICommand CreateCommandFor(IInteractableElement caster, Vector3Int td, ref object extCache);
    bool IsPhaseCommand(ICommand cmd);
    Vector3Int GetBorderCell(Vector3Int extensionCell, Tilemap tilemap);
}
