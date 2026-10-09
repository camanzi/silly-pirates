using System.Collections.Generic;

/// <summary>
/// An equipment ability that can be fired by the ship's Broadside, with no crew member driving it.
///
/// The player-facing <see cref="AbilityBase.CreateCommand"/> reads the targets from the shared
/// SelectionContextSO; the broadside instead hands over its own list, picked automatically by the
/// equipment's <see cref="BroadsideTargetSelectorSO"/>. Readiness (<see cref="AbilityBase.CanExecute"/>) is
/// deliberately bypassed: it checks the player's selection, which plays no part here. Per-target legality
/// still goes through <see cref="AbilityBase.IsValidTarget"/> (see <see cref="BroadsideCandidateQuery"/>).
/// </summary>
public interface IBroadsideAbility
{
    /// <param name="targets">Owned by the command from now on: pass a fresh list, never a shared one.</param>
    ICommand CreateBroadsideCommand(IInteractableElement caster, List<ITargettable> targets);
}
