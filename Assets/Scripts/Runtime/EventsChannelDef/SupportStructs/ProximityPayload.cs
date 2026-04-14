using System.Collections.Generic;
using UnityEngine;

public struct ProximityPayload {
    public GridCharacter ActiveCharacter;
    public int Range;

    public ProximityPayload(GridCharacter activeCharacter, int range)
    {
        this.ActiveCharacter =activeCharacter;
        this.Range = range;
    }

    public static ProximityPayload Empty => new (null, -1);
}