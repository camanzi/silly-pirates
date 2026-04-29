
using System;

public interface IAwakable 
{
    public int MaxAwakeningPoints { get; }
    public int CurrentAwakeningPoints { get; }
    public int AwakeningPoints { get; set; }
    public bool IsAwake { get; }
    public bool IsOnCooldown { get; }
    public int Cooldown { get; set; }
    
    public void AddAwakeningPoints(int count);
    public void RemoveAwakeningPoints(int count);
    public void ConsumeAllAwakeningPoints();
    Action OnDataChanged { get; set; }
}
