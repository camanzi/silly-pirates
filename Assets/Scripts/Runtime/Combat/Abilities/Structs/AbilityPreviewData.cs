using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct AbilityPreviewData
{
    public List<Vector3> AffectedCells;    
    public Vector3? FreeAimTarget;

    public AbilityPreviewData(List<Vector3> affectedCells, Vector3? freeAimTarget = null)
    {
        this.AffectedCells = affectedCells;
        this.FreeAimTarget = freeAimTarget;
    }      

    public static AbilityPreviewData Empty => new AbilityPreviewData { 
        AffectedCells = new (), 
        FreeAimTarget = null 
    };
}
