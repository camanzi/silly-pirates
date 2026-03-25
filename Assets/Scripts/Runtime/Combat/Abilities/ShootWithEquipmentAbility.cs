using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "Shoot With Equipment Ability", menuName = "Abilities/Equipment/Shoot With Equipment Ability")]
public class ShootWithEquipmentAbility : AbilityBase
{
    public override bool CanExecute(GridElement caster, Vector3Int target)
    {
        return false;
    }

    public override ICommand CreateCommand(GridElement caster, Vector3Int targetCell, List<GridElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    // Uso il mouse in quanto devoi mirare il ray cast del mouse come preview
    public override AbilityPreviewData GetPreviewData(Vector3 target, GridElement caster)
    {
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.green, 0.5f);

        Debug.Log($"Il raycast é partito da {ray.origin} verso {ray.direction}");

        if(Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Debug.Log($"Ho colpito {hitInfo.collider.name}");

            List<Transform> targetObjs = new() { hitInfo.collider.transform };
            return new AbilityPreviewData(targetObjs);
        }

        return AbilityPreviewData.Empty;
    }
}
