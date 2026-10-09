
using System;

public interface IAwakable
{
    /// <summary>The points needed to awaken the equipment. Points never go beyond this value.</summary>
    public int MaxAwakeningPoints { get; }
    public int CurrentAwakeningPoints { get; }
    public bool IsAwake { get; }
    public bool IsOnCooldown { get; }
    public int Cooldown { get; set; }

    /// <summary>Adds points, clamped to <see cref="MaxAwakeningPoints"/>.</summary>
    public void AddAwakeningPoints(int count);
    public void RemoveAwakeningPoints(int count);
    public void ConsumeAllAwakeningPoints();
    Action OnAwakeningCountersChanged { get; set; }
    Action<int> OnCooldownChanged { get; set; }
    Action<int> OnAwakeningHoverPreview { get; set; }
}
