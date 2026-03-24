using System.Collections.Generic;
using UnityEngine;

public struct HighlightTargetsPayload
{
    public HighlightTargetsPayload(ICollection<Transform> targets, bool isValidHighlight)
    {
        this.targets = targets;
        this.isValidHighlight = isValidHighlight;
    }

    public ICollection<Transform> targets;
    public bool isValidHighlight;
}
