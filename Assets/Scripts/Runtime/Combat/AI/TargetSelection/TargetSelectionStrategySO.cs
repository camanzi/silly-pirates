using System.Collections.Generic;
using UnityEngine;

public abstract class TargetSelectionStrategySO : ScriptableObject
{
    public abstract List<ITargettable> SelectTargets(AIContext context, int count);
}
