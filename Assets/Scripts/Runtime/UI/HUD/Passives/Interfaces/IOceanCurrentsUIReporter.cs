using System;

public interface IOceanCurrentsUIReporter
{
    bool IsAvailable { get; }
    event Action OnStateUpdated;
}
