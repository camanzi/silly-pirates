using System;
using UnityEngine;

public class CharacterTurnData
{
    public ITurnAgent Agent;
    public int ActionValue;
    public Sprite Icon;
    public bool IsSubTurn;

    public CharacterTurnData(ITurnAgent agent, float av, Sprite icon, bool isSubTurn = false)
    {
        this.Agent = agent;
        this.ActionValue = (int)av;
        this.Icon = icon;
        this.IsSubTurn = isSubTurn;
    }
}