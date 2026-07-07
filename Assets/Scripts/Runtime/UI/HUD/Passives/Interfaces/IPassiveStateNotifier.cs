using System;

public interface IPassiveStateNotifier
{
    event Action OnStateChanged;
}
