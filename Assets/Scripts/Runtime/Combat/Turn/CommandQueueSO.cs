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

        // finally obbligatorio: senza, un'eccezione da un qualsiasi comando lascerebbe _isProcessing
        // a true per sempre e questa SO — condivisa fra tutti i turni — rifiuterebbe silenziosamente
        // ogni comando successivo.
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
