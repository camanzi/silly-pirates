using System;
using UnityEngine;

public class CharacterTurnData
{
    public ITurnAgent Agent;
    public int ActionValue;
    public Sprite Icon;

    public CharacterTurnData(ITurnAgent agent, float av, Sprite icon)
    {
        this.Agent = agent;
        this.ActionValue = (int)av;
        this.Icon = icon;
    }
}