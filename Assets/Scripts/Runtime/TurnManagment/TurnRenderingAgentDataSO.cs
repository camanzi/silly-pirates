using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Turn System/Rendering Agent Data" )]
public class TurnRenderingAgentDataSO : ScriptableObject
{
    [Header("Turn Rendering Agent Data Config")]
    [Tooltip("Icon to be shown in turn list")]
    [SerializeField] private Sprite _turnAgentIcon;

    public Sprite TurnAgentIcon => _turnAgentIcon;
}
