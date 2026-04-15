
public interface IAwakable 
{
    public int AwakingPoints { get; }
    public bool IsAwake { get; }
    public void AddAwakingPoints(int count);
    public void RemoveAwakingPoints(int count);
    public void ConsumeAllAwakingPoints();
}
