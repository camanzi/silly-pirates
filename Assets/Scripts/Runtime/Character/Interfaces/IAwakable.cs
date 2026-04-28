
using System;

public interface IAwakable 
{
    public int MaxAwakeningPoints { get; }
    public int CurrentAwakingPoints { get; }
    public bool IsAwake { get; }
    public EAwakeningStatus AwakeningStatus { get; set; }
    public int Cooldown { get; set; }
    
    public void AddAwakingPoints(int count);
    public void RemoveAwakingPoints(int count);
    public void ConsumeAllAwakingPoints();
    Action OnDataChanged { get; set; }
}
