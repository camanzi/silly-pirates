using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the trajectory arcs while aiming. The arc renderers are borrowed from a pool rather than
/// destroyed: the preview is redrawn on every pointer movement, and the valid-hover -> invalid-hover ->
/// valid-hover cycle would recreate the GameObjects over and over.
/// </summary>
public class FreeAimRenderer : MonoBehaviour
{
    [Header("External References")]
    [SerializeField] private TrajectoryRenderer _trajectoryRenderer;

    [Header("Pool")]
    [Min(0)] [SerializeField] private int _prewarmSize = 2;
    [Tooltip("Cap on how many arcs can be drawn at once. Beyond it, the surplus arcs are not shown")]
    [Min(1)] [SerializeField] private int _maxPoolSize = 8;

    private readonly List<TrajectoryRenderer> _arcRenderers = new();

    private ComponentPool<TrajectoryRenderer> _pool;
    private Transform _poolRoot;

    private void Awake()
    {
        if (_trajectoryRenderer == null)
        {
            Debug.LogError($"[FreeAimRenderer] No TrajectoryRenderer prefab assigned on '{name}'.", this);
            return;
        }

        _poolRoot = new GameObject("TrajectoryPool").transform;
        _poolRoot.SetParent(transform, false);

        _pool = new ComponentPool<TrajectoryRenderer>(_trajectoryRenderer, _poolRoot, _prewarmSize, _maxPoolSize);
    }

    private void OnDisable() => ClearRenderers();

    private void OnDestroy() => _pool?.Clear();

    public void HighlightTargets(HighlightFreeAimPayload payload)
    {
        if (_pool == null) return;

        List<TrajectoryArc> arcs = payload.Arcs;

        if (arcs == null || arcs.Count == 0 || payload.Equals(HighlightFreeAimPayload.Empty))
        {
            ClearRenderers();
            return;
        }

        while (_arcRenderers.Count < arcs.Count)
        {
            TrajectoryRenderer renderer = _pool.Acquire();
            if (renderer == null) break;   // pool exhausted: only the arcs that fit are drawn
            _arcRenderers.Add(renderer);
        }

        while (_arcRenderers.Count > arcs.Count)
        {
            _pool.Release(_arcRenderers[^1]);
            _arcRenderers.RemoveAt(_arcRenderers.Count - 1);
        }

        for (int i = 0; i < _arcRenderers.Count; i++)
            _arcRenderers[i].HighlightTrajectory(arcs[i].Start, arcs[i].End, arcs[i].PeakHeight, Color.white);
    }

    private void ClearRenderers()
    {
        for (int i = 0; i < _arcRenderers.Count; i++)
            _pool?.Release(_arcRenderers[i]);

        _arcRenderers.Clear();
    }
}
