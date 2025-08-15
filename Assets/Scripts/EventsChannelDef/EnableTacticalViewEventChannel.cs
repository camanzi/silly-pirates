using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Activate Tactical View Event Channel", menuName = "Events/Activate Tactical View Event Channel")]
public class EnableTacticalViewEventChannel : GenericEventChannelSO<bool>
{
    [Header("Default configurations")]
    [SerializeField] private bool defaultIsActive = false;
    private bool isActive;

    private void OnEnable()
    {
        isActive = defaultIsActive;
    }

    public void RaiseEvent() 
    {
        isActive = !isActive;
        base.RaiseEvent(isActive);
    }

    public override void RaiseEvent(bool payload)
    {
        isActive = payload;
        base.RaiseEvent(payload);
    }
}
