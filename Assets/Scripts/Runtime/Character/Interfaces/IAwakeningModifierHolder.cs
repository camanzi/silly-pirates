using System;

public interface IAwakeningModifierHolder
{
    int TotalAwakeningBonus { get; }
    void AddAwakeningModifier(IAwakeningModifier modifier);
    void RemoveAwakeningModifier(IAwakeningModifier modifier);
    void NotifyAwakeningContribution(IAwakable awakable);
    event Action OnAwakeningModifiersChanged;
}
