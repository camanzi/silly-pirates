using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public struct HighlightPayload
{
    public List<Vector3> targets;
    public bool isValidHighlight;
    public Vector3? origin;
    public HighlightPayload(List<Vector3> targets, bool isValidHighlight, Vector3 origin)
    {
        this.targets = targets;
        this.isValidHighlight = isValidHighlight;
        this.origin = origin;
    }

    public HighlightPayload(List<Vector3> targets, bool isValidHighlight)
    {
        this.targets = targets;
        this.isValidHighlight = isValidHighlight;
        this.origin = null;
    }

    public static HighlightPayload Empty => new ()
    {
        isValidHighlight = false,
        targets = new() 
    };
}
