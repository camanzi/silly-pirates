using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cell Cost Registry", menuName = "Grid/Cell Cost Registry")]
public class CellCostRegistrySO : ScriptableObject
{
    private readonly List<ICellCostModifier> _modifiers = new();

    // Guardia anti-duplicati: senza, un Register ripetuto sullo stesso modificatore (ricompilazione
    // script in Play Mode, o una ri-registrazione difensiva dopo ResetForNewCombat) fa sì che
    // GetMovementCost sommi lo stesso contributo più volte.
    public void Register(ICellCostModifier modifier)
    {
        if (modifier == null || _modifiers.Contains(modifier)) return;
        _modifiers.Add(modifier);
    }

    public void Unregister(ICellCostModifier modifier) => _modifiers.Remove(modifier);

    public int GetMovementCost(Vector3Int cell)
    {
        int cost = 1;
        for (int i = 0; i < _modifiers.Count; i++)
            cost += _modifiers[i].GetAdditionalCost(cell);
        return Mathf.Max(0, cost);
    }

    // DELIBERATAMENTE non implementa ICombatSessionResettable, e non va messo nella lista
    // _resettables di CombatSessionSO. I registrant di oggi (PathOfStarDataSO, SlimyCellDataSO) sono
    // asset SO: vivono quanto l'applicazione e non diventano mai riferimenti morti, quindi svuotare
    // la lista tra un combattimento e l'altro non ripara nulla e anzi cancellerebbe registrazioni
    // ancora valide — il loro OnEnable non riscatta (l'asset è già in memoria) e il contributo al
    // costo sparirebbe per il resto della sessione. La correttezza sui load ripetuti viene dal
    // protocollo simmetrico Register/Unregister, come per RuntimeAnchorSO.
    // Se un domani un modificatore fosse un oggetto di scena, deve deregistrarsi nel proprio
    // OnDisable: è quello il contratto, non un reset centralizzato.
}
