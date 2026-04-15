using UnityEngine;
using UnityEngine.EventSystems;

public interface IAwakable 
{
    public int AwakingPoints { get; }
    public bool IsAwake();
    public void AddAwakingPoints(int count);
    public void RemoveAwakingPoints(int count);
}
