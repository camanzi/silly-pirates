using UnityEngine;

public interface ICameraCueHandler
{
    Awaitable RunAsync(CameraCueContext context);
}
