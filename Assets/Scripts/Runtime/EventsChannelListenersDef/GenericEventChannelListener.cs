using UnityEngine;
using UnityEngine.Events;

public class GenericEventChannelListener<TEventChannel, TEventType> : MonoBehaviour where TEventChannel : GenericEventChannelSO<TEventType>
{
    [Header("Listen to event channel")]
    [SerializeField] protected TEventChannel eventChannel;
    [Tooltip("Actions to invoke when channel fire an event")]
    [SerializeField] protected UnityEvent<TEventType> response;

    protected virtual void OnEnable()
    {
        if (eventChannel != null) 
        {
            eventChannel.OnEventRaised += OnEventRaised;
        }
    }

    protected virtual void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnEventRaised -= OnEventRaised;
        }
    }

    public void OnEventRaised(TEventType evt) 
    {
        response?.Invoke(evt);
    }
}

public class GenericEventChannelListener<TEventChannel> : MonoBehaviour where TEventChannel : GenericEventChannelSO
{
    [Header("Listen to event channel")]
    [SerializeField] protected TEventChannel eventChannel;
    [Tooltip("Actions to invoke when channel fire an event")]
    [SerializeField] protected UnityEvent response;

    protected virtual void OnEnable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnEventRaised += OnEventRaised;
        }
    }

    protected virtual void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnEventRaised -= OnEventRaised;
        }
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}