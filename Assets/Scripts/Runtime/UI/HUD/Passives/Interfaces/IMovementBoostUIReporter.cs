using System;
using UnityEngine;

public interface IMovementBoostUIReporter 
{
    int CurrentStacks { get; }
    int MaxStacks { get; }
    int TurnsUntilDecay { get; }
    event Action OnStateUpdated;
}
