using System;
using UnityEditor;
using UnityEngine;

// Esistenza momentanea diventerá poi un SO
[System.Serializable]
public class CharacterTurnData
{
    public Guid characterID;
    public int actionValue;
    public Sprite icon;
    
    public CharacterTurnData(int actionValue, Sprite icon)
    {
        this.characterID = Guid.NewGuid();
        this.actionValue = actionValue;
        this.icon = icon;
    }
}