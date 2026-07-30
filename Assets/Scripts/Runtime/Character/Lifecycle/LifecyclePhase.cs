/// <summary>
/// Fase del ciclo di vita di un personaggio a cui può essere agganciata un'animazione.
/// Enum estendibile (es. Revive, TurnStart in futuro) — usato come chiave di dizionario,
/// quindi i valori esistenti non vanno mai rinumerati.
/// </summary>
public enum LifecyclePhase
{
    Spawn,
    Leave
}
