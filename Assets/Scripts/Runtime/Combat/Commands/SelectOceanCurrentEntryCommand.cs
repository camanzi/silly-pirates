using UnityEngine;

public class SelectOceanCurrentEntryCommand : ICommand
{
    public async Awaitable ExecuteAsync()
    {
        await Awaitable.NextFrameAsync();
    }

    public void Undo() { }
}
