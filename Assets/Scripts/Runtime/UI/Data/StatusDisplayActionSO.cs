using UnityEngine;

/// <summary>
/// A display-only action for the centre of an equipment menu: it lends its icon to the status ring and can
/// never be executed nor shown among the radial buttons. Used by offensive equipment, which the crew can only
/// awaken — the firing happens in the ship's broadside (see <see cref="ShipBroadsideController"/>).
/// </summary>
[CreateAssetMenu(fileName = "StatusDisplayActionSO", menuName = "UI/Interactions/Equipment/Status Display Action")]
public class StatusDisplayActionSO : InteractionActionSO
{
    public override bool ExecuteAction(IInteractableElement element, ITurnAgent interactingAgent) => false;

    public override bool CanExecute(IInteractableElement element, ITurnAgent interactingAgent) => false;

    public override bool CanShow(IInteractableElement element, ITurnAgent interactingAgent) => false;
}
