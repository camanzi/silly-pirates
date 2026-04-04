using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct AbilityPreviewData
{
    public List<Vector3> AffectedCells;    
    public List<ITargettable> FreeAimTargets;

    public AbilityPreviewData(List<Vector3> affectedCells, List<ITargettable> freeAimTargets = null)
    {
        this.AffectedCells = affectedCells;
        this.FreeAimTargets = freeAimTargets;
    }      

    public static AbilityPreviewData Empty => new AbilityPreviewData { 
        AffectedCells = new (), 
        FreeAimTargets = null 
    };
}
