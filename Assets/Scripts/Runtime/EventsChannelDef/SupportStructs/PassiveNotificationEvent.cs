using UnityEngine;

public struct PassiveNotificationEvent
{
    public string DisplayName;
    public bool WasAdded;
    // 0 quando la passiva non espone IStackCountProvider.
    public int StackCount;
    public Vector3 WorldPosition;
    public Transform Source;
    // Null quando la passiva non ha un'icona assegnata in PassiveAbilitySO.
    public Sprite Icon;
}
