[System.Serializable]
public class EntityTurnState
{
    public ITurnAgent Agent;
    public float CurrentAV;
    
    public EntityTurnState(ITurnAgent a, float av) { Agent = a; CurrentAV = av; }
}