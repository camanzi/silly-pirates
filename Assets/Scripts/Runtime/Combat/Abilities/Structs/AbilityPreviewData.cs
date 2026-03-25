using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct AbilityPreviewData
{
    public List<Vector3> affectedCells;    
    public List<Vector3> freeAimTargets;

    public AbilityPreviewData(List<Vector3> affectedCells, List<Vector3> freeAimTargets)
    {
        this.affectedCells = affectedCells;
        this.freeAimTargets = freeAimTargets;
    }      

    public static AbilityPreviewData Empty => new AbilityPreviewData { 
        affectedCells = new (), 
        freeAimTargets = new () 
    };
}
