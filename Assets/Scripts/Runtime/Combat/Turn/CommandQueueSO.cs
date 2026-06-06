using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CommandQueue", menuName = "Combat/Turn System/Command Queue")]
public class CommandQueueSO : ScriptableObject
{
    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    public void Clear()
    {
        _commandQueue.Clear();
        _isProcessing = false;
    }

    public void AddCommand(ICommand command) => _commandQueue.Enqueue(command);

    public async Awaitable ProcessQueueAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        while (_commandQueue.Count > 0)
        {
            ICommand cmd = _commandQueue.Dequeue();
            await cmd.ExecuteAsync();
        }

        _isProcessing = false;
    }
}
