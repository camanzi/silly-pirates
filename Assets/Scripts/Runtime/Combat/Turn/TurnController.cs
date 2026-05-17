using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TurnController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private HighlightGridEventChannel _highlightCellsEventChannel;
    [SerializeField] private HighlightFreeAimEventChannel _targetTransformEventChannel;
    [SerializeField] private TurnAgentEventChannel _onJoinFightAgentEventChannel;

    [Header("Preview Tiles")]
    [SerializeField] private Tile _hoverFloorAllowTile;
    [SerializeField] private Tile _hoverFloorNotAllowTile;
    [SerializeField] private Tile _inRangePreviewTile;

    [Header("Turn Managments")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private TurnStateSO _currentTurnState;
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;

    private Queue<ICommand> _commandQueue = new Queue<ICommand>();
    private bool _isProcessing = false;

    // FIXME Later Idealmente quando INIZIA un fight bisogna chiamare questa cosa
    protected async Awaitable OnEnable()
    {
        _turnOrderData.Clear();
        _currentTurnState.Clear();

        await Awaitable.NextFrameAsync();
        _ = RunGameLoop(destroyCancellationToken);
    }

    #region ABILITY PREVIEW
    public void DrawAbilityPreview(AbilityPreviewData data, AbilityBase ability, IInteractableElement caster, TargetingData targetingData, bool canExecute)
    {
        List<TrajectoryArc> arcs = null;
        if (data.Arcs != null)
        {
            arcs = data.Arcs;
        }
        else if (ability.ShowTrajectory)
        {
            float height = ability.TrajectoryConfigData?.Height ?? 0f;
            arcs = new List<TrajectoryArc>();
            foreach (ITargettable target in data.FreeAimTargets ?? new())
                arcs.Add(new TrajectoryArc { Start = caster.Transform.position, End = target.Transform.position, PeakHeight = height });
            if (targetingData.worldPosition.HasValue)
                arcs.Add(new TrajectoryArc { Start = caster.Transform.position, End = targetingData.worldPosition.Value, PeakHeight = height });
        }

        if (arcs != null)
            _targetTransformEventChannel.RaiseEvent(new HighlightFreeAimPayload(caster, canExecute, data.FreeAimTargets, arcs));

        var layers = new List<CellOverlayLayer>();
        if (data.InteractionArea != null)
            layers.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewInteraction, Cells = ToVector3IntList(data.InteractionArea), Tile = _inRangePreviewTile, Target = TilemapTarget.Preview });
        layers.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewAffected, Cells = ToVector3IntList(data.AffectedCells), Tile = canExecute ? _hoverFloorAllowTile : _hoverFloorNotAllowTile, Target = TilemapTarget.Preview });
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload { Layers = layers });
    }

    public void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload
        {
            Layers = new List<CellOverlayLayer>
            {
                new() { Key = HighlightLayerKeys.PreviewAffected,    Target = TilemapTarget.Preview },
                new() { Key = HighlightLayerKeys.PreviewInteraction, Target = TilemapTarget.Preview }
            }
        });
        _targetTransformEventChannel.RaiseEvent(HighlightFreeAimPayload.Empty);
    }

    private static List<Vector3Int> ToVector3IntList(List<Vector3> list)
    {
        if (list == null) return null;
        var result = new List<Vector3Int>(list.Count);
        foreach (var v in list) result.Add(Vector3Int.FloorToInt(v));
        return result;
    }
    #endregion

    #region COMMANDS HANDLING
    public void AddCommand(ICommand command) => _commandQueue.Enqueue(command);

    public async Awaitable ProcessQueueAsync() {
        if (_isProcessing) return;
        _isProcessing = true;

        while (_commandQueue.Count > 0) {
            ICommand cmd = _commandQueue.Dequeue();
            await cmd.ExecuteAsync(); 
        }

        _isProcessing = false;
    }
    #endregion

    #region TURN AGENT HANDLING & CORE GAME LOOP
    public async Awaitable RunGameLoop(CancellationToken token)
    {
        try 
        {
            await Awaitable.NextFrameAsync(token);

            while (!token.IsCancellationRequested)
            {
                if (_turnOrderData.TurnQueue.Count == 0)
                {
                    await Awaitable.NextFrameAsync(token);
                    continue;
                }

                ITurnAgent nextEntity = _turnOrderData.TurnQueue[0].Agent;

                _turnOrderData.StartActiveTurn();

                _currentTurnState.SetActiveCharacter(nextEntity);
                nextEntity.OnStartingTurn();
                
                await _currentTurnState.WaitUntilTurnFinished();

                _onAnyTurnEnded?.RaiseEvent(nextEntity);
                _turnOrderData.CompleteActiveTurn();
            }
        } 
        catch (OperationCanceledException)
        {
            Debug.Log("Turn Loop cancellato correttamente.");
        }
    }

    public void OnAgentJoin(ITurnAgent agent) => _turnOrderData.AddEntity(agent);

    public void OnAgentLeave(ITurnAgent agent)
    {
        _turnOrderData.RemoveEntity(agent);
        
        if (_currentTurnState.ActiveAgent == agent)
        {
            _currentTurnState.SignalTurnEnd();
        }
    }
    #endregion
}
