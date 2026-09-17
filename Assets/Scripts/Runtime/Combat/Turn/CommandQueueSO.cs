using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CommandQueue", menuName = "Combat/Turn System/Command Queue")]
public class CommandQueueSO : ScriptableObject, ICombatSessionResettable
{
    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    public void Clear()
    {
        _commandQueue.Clear();
        _isProcessing = false;
    }

    public void ResetForNewCombat() => Clear();

    public void AddCommand(ICommand command) => _commandQueue.Enqueue(command);

    public async Awaitable ProcessQueueAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        // The finally is mandatory: without it an exception from any command would leave _isProcessing
        // true forever, and this SO — shared across every turn — would silently reject every subsequent
        // command.
        try
        {
            while (_commandQueue.Count > 0)
            {
                ICommand cmd = _commandQueue.Dequeue();
                await cmd.ExecuteAsync();
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
