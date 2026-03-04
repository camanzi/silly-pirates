using UnityEngine;

public interface ICommand {
    Awaitable ExecuteAsync();
    void Undo();
}