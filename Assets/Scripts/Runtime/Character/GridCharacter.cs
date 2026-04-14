using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;

public class GridCharacter : InteractableGridElement, IMovable, ITargettable, ITurnAgent
{
    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;

    public TurnAgentDataSO AgentData => _agentData;
    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;

    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;

    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;

    private DirectionalSpriteController _directionalSpriteController;
    private Tween _moveTween;

    protected override void Awake()
    {
        base.Awake();
        _directionalSpriteController = GetComponent<DirectionalSpriteController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OnCombatJoin();
    }

    public async Awaitable MoveTo(IEnumerable<Vector3> path, CancellationToken token)
    {
        _moveTween.Stop();
    
        _directionalSpriteController.PlayAnimation("human_walk");

        foreach (Vector3 node in path)
        {
            if (token.IsCancellationRequested) break;

            Vector3 targetPosition = _floorTilemap.GetCellCenterWorld(Vector3Int.FloorToInt(node));

            transform.LookAt(targetPosition);
            await Tween.Position(transform, targetPosition, duration: 0.5f, ease: Ease.Linear);
            
            gridPosition = Vector3Int.FloorToInt(node);

            _proximityChannel.RaiseEvent(new ProximityPayload(this, _agentData.InteractionRange));
        }

        _directionalSpriteController.PlayAnimation("human_idle");
    }

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave() => this.HandleCombatLeave();

    public void OnStartingTurn() => this.HandleStartingTurn();
}
