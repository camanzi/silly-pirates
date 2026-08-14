/// <summary>
/// Contratto minimo per ogni ScriptableObject che porta stato runtime non serializzato e destinato
/// a sopravvivere fra un caricamento dell'asset e l'altro (campi privati, HashSet/Dictionary statici,
/// contatori). In un'architettura multiscena additiva questi campi NON vengono azzerati dallo scarico
/// della scena di combattimento: OnEnable di uno ScriptableObject è un hook di caricamento asset in
/// memoria, non di caricamento scena, quindi scatta una sola volta per sessione di Play/build.
/// CombatSessionSO invoca ResetForNewCombat() su tutti gli implementatori registrati, fra lo scarico
/// della scena vecchia e il carico di quella nuova.
/// </summary>
public interface ICombatSessionResettable
{
    void ResetForNewCombat();
}
