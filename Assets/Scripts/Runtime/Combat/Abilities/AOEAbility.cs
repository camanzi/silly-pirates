using System.Collections.Generic;
using UnityEngine;

public abstract class AOEAbility : AbilityBase {
    [SerializeField] private ShapeType shapeType;
    [SerializeField] private int radius;

    public override bool CanExecute(GridElement caster, Vector3Int target)
    {
        throw new System.NotImplementedException();
    }

    public override ICommand CreateCommand(GridElement caster, Vector3Int target, List<GridElement> targetsInArea)
    {
        throw new System.NotImplementedException();
    }

    public override AbilityPreviewData GetPreviewData(Vector3Int target, GridElement caster) {
        IAreaShape shapeEvaluator = ShapeFactory.GetShape(shapeType);
        return new AbilityPreviewData(shapeEvaluator.GetCells(target, radius, caster.gridPosition));
    }
}
