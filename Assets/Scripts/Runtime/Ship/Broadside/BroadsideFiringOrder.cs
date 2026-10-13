using System;
using System.Collections.Generic;

/// <summary>
/// Pure ordering logic of the Broadside: splits the planned shots into waves by firing priority.
/// Kept free of scene types so it can be tested without a ship.
/// </summary>
public static class BroadsideFiringOrder
{
    /// <summary>
    /// Fills <paramref name="waves"/> with one list per distinct priority, in ascending priority. Stable: inside
    /// a wave the items keep the order they have in <paramref name="items"/> (the awakening order). Gaps between
    /// priorities are allowed and produce no empty wave. <paramref name="waves"/> is cleared first; every inner
    /// list is new.
    /// </summary>
    public static void BuildWaves<T>(IReadOnlyList<T> items, Func<T, int> priorityOf, List<List<T>> waves)
    {
        if (waves == null) throw new ArgumentNullException(nameof(waves));
        if (priorityOf == null) throw new ArgumentNullException(nameof(priorityOf));

        waves.Clear();
        if (items == null || items.Count == 0) return;

        // Distinct priorities in ascending order. Few items and few zones: a sorted list is enough.
        var priorities = new List<int>();
        var itemPriorities = new int[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            int priority = priorityOf(items[i]);
            itemPriorities[i] = priority;

            int index = priorities.BinarySearch(priority);
            if (index < 0) priorities.Insert(~index, priority);
        }

        for (int p = 0; p < priorities.Count; p++)
            waves.Add(new List<T>());

        // A single forward pass per item keeps the original order inside each wave.
        for (int i = 0; i < items.Count; i++)
            waves[priorities.BinarySearch(itemPriorities[i])].Add(items[i]);
    }
}
