using UnityEngine;

[CreateAssetMenu(fileName = "NewInteraction", menuName = "UI/Interactions/Action")]
public class InteractionActionSO : ScriptableObject
{
    [SerializeField] private string _actionName;
    [SerializeField] private Sprite _icon;

    public string ActionName => _actionName;
    public Sprite Icon => _icon;
}
