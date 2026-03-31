using System.Collections.Generic;
using UnityEngine;

public abstract class AOEAbility : AbilityBase {
    [SerializeField] private ShapeType shapeType;
    [SerializeField] private int radius;

    public override bool CanExecute(IInteractableElement caster, Vector3 target)
    {
        throw new System.NotImplementedException();
    }

    public override ICommand CreateCommand(IInteractableElement caster, Vector3 target, List<IInteractableElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(Vector3 target, IInteractableElement caster) {
        if (caster is not GridElement gridElement) return AbilityPreviewData.Empty;

        IAreaShape shapeEvaluator = ShapeFactory.GetShape(shapeType);
        return new AbilityPreviewData(affectedCells: shapeEvaluator.GetCells(Vector3Int.FloorToInt(target), radius, gridElement.gridPosition), freeAimTargets: new());
    }
}
