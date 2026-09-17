using UnityEngine;

public struct PassiveNotificationEvent
{
    public string DisplayName;
    public bool WasAdded;
    // 0 when the passive does not expose IStackCountProvider.
    public int StackCount;
    public Vector3 WorldPosition;
    public Transform Source;
    // Null when the passive has no icon assigned on its PassiveAbilitySO.
    public Sprite Icon;
}
