using System.Collections.Generic;
using UnityEngine;

public struct HighlightTargetsPayload
{
    public HighlightTargetsPayload(ICollection<Vector3> targets, bool isValidHighlight)
    {
        this.targets = targets;
        this.isValidHighlight = isValidHighlight;
    }

    public ICollection<Vector3> targets;
    public bool isValidHighlight;
}
