using UnityEngine;

// Esistenza momentanea diventerá poi un SO
[System.Serializable]
public class CharacterTurnData
{
    public string characterName;
    public Sprite characterIcon;
    public int currentHP;
    public int maxHP;
    public int initiative; // Per ordinamento turni
    
    public CharacterTurnData(string name, Sprite icon, int hp, int maxHp, int init = 0)
    {
        characterName = name;
        characterIcon = icon;
        currentHP = hp;
        maxHP = maxHp;
        initiative = init;
    }
}