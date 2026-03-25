using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct AbilityPreviewData
{
    public List<Vector3Int> affectedCells;    
    public List<Transform> targets;

    public AbilityPreviewData(List<Vector3Int> affectedCells, List<Transform> targets)
    {
        this.affectedCells = affectedCells;
        this.targets = targets;
    }    

    public AbilityPreviewData(List<Vector3Int> affectedCells)
    {
        this.affectedCells = affectedCells;
        this.targets = new();
    }    

    public AbilityPreviewData(List<Transform> targets)
    {
        this.affectedCells = new();
        this.targets = targets;
    }    


    public static AbilityPreviewData Empty => new AbilityPreviewData { 
        affectedCells = new (), 
        targets = new () 
    };
}
