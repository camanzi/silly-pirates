using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disegna gli archi di traiettoria durante la mira. Gli arc renderer vengono prestati da un pool
/// e non distrutti: l'anteprima viene ridisegnata a ogni movimento del puntatore, e il ciclo
/// hover-valido -> hover-non-valido -> hover-valido ricreerebbe i GameObject continuamente.
/// </summary>
public class FreeAimRenderer : MonoBehaviour
{
    [Header("External References")]
    [SerializeField] private TrajectoryRenderer _trajectoryRenderer;

    [Header("Pool")]
    [Min(0)] [SerializeField] private int _prewarmSize = 2;
    [Tooltip("Tetto di archi disegnabili insieme. Oltre questo gli archi in eccesso non vengono mostrati")]
    [Min(1)] [SerializeField] private int _maxPoolSize = 8;

    private readonly List<TrajectoryRenderer> _arcRenderers = new();

    private ComponentPool<TrajectoryRenderer> _pool;
    private Transform _poolRoot;

    private void Awake()
    {
        if (_trajectoryRenderer == null)
        {
            Debug.LogError($"[FreeAimRenderer] Nessun prefab di TrajectoryRenderer assegnato su '{name}'.", this);
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
            if (renderer == null) break;   // pool esaurita: si disegnano gli archi che ci stanno
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
