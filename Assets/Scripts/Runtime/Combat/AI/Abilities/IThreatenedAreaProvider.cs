using System.Collections.Generic;
using UnityEngine;

public interface IThreatenedAreaProvider
{
    bool TryGetThreatenedWorldPoints(HostileCharacter caster, out IReadOnlyList<Vector3> points);
}
