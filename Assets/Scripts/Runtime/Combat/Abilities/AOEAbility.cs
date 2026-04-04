using System.Collections.Generic;
using UnityEngine;

public abstract class AOEAbility : AbilityBase {
    [SerializeField] private ShapeType shapeType;
    [SerializeField] private int radius;

    public override bool CanExecute(IInteractableElement caster, TargetingData targetingData)
    {
        throw new System.NotImplementedException();
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData targetingData)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(TargetingData targetingData, IInteractableElement caster) {
        if (caster is not GridElement gridElement) return AbilityPreviewData.Empty;

        Vector3 target = targetingData.cellPosition;
        IAreaShape shapeEvaluator = ShapeFactory.GetShape(shapeType);
        return new AbilityPreviewData(affectedCells: shapeEvaluator.GetCells(Vector3Int.FloorToInt(target), radius, gridElement.gridPosition));
    }
}
