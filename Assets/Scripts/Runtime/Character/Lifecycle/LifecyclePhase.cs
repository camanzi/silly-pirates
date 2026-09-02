/// <summary>
/// Fase del ciclo di vita di un personaggio a cui può essere agganciata un'animazione.
/// Enum estendibile (es. Revive, TurnStart in futuro) — usato come chiave di dizionario,
/// quindi i valori esistenti non vanno mai rinumerati.
///
/// <see cref="Spawn"/> e <see cref="Leave"/> sono transizioni: finiscono da sole e passano da
/// <see cref="CharacterLifecycleAnimator.PlayAsync"/>. <see cref="Idle"/> è la prima fase IN LOOP: non
/// finisce da sola, gira su <see cref="CharacterLifecycleAnimator.StartLoop"/> finché non viene sostituita.
/// Una futura <c>PostDeath</c> in loop si aggiungerebbe qui allo stesso modo.
/// </summary>
public enum LifecyclePhase
{
    Spawn,
    Leave,
    Idle
}
