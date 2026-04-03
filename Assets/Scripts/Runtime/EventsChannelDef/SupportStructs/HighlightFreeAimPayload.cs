using System.Collections.Generic;
using UnityEngine;

public struct HighlightFreeAimPayload
{
    public Vector3? Target;
    public bool IsValidHighlight;
    public Vector3 Origin;
    public HighlightFreeAimPayload(Vector3 origin, bool isValidHighlight, Vector3? target = null)
    {
        this.IsValidHighlight = isValidHighlight;
        this.Origin = origin;
        this.Target = target;
    }

    public static HighlightFreeAimPayload Empty => new ()
    {
        IsValidHighlight = false,
        Target = null 
    };
}
