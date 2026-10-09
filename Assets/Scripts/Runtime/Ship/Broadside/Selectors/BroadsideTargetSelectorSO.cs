using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The swappable criterion an <see cref="OffensiveEquipment"/> uses to pick its broadside targets. Assigned
/// per equipment from the Inspector, so each cannon can aim by its own rule.
///
/// Stateless template: never keep per-combat data in a subclass, the same asset is shared by every
/// equipment that references it. A future scoring selector (e.g. a list of weighted scorer SOs) fits this
/// contract as it is.
/// </summary>
public abstract class BroadsideTargetSelectorSO : ScriptableObject
{
    /// <summary>
    /// Fills <paramref name="results"/> (cleared first) with at most <see cref="BroadsideTargetingContext.TargetCount"/>
    /// targets, taken from <see cref="BroadsideTargetingContext.Candidates"/>. An empty list means "nothing to shoot".
    /// </summary>
    public abstract void SelectTargets(in BroadsideTargetingContext context, List<ITargettable> results);
}
