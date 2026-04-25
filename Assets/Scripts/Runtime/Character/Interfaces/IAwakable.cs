
using System;

public interface IAwakable 
{
    public int MaxAwakeningPoints { get; }
    public int CurrentAwakingPoints { get; }
    public bool IsAwake { get; }
    public void AddAwakingPoints(int count);
    public void RemoveAwakingPoints(int count);
    public void ConsumeAllAwakingPoints();
    Action OnDataChanged { get; set; }
}
