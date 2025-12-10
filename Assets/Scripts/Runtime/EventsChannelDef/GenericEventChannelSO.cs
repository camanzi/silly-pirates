using UnityEngine;
using UnityEngine.Events;

public abstract class GenericEventChannelSO<T> : ScriptableObject
{
    [Tooltip("Actions to perform; Listeners subscribe to this UnityAction")]
    public UnityAction<T> OnEventRaised;

    public virtual void RaiseEvent(T payload) 
    {
        OnEventRaised?.Invoke(payload);
    }
}

public abstract class GenericEventChannelSO : ScriptableObject
{
    [Tooltip("Actions to perform; Listeners subscribe to this UnityAction")]
    public UnityAction OnEventRaised;

    public virtual void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}