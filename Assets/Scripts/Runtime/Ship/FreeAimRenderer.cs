using System.Collections.Generic;
using UnityEngine;

public class FreeAimRenderer : MonoBehaviour
{
    [Header("External References")]
    [SerializeField] private TrajectoryRenderer _trajectoryRenderer;

    private List<TrajectoryRenderer> _arcRenderers = new();

    public void HighlightTargets(HighlightFreeAimPayload payload)
    {
        if (payload.Equals(HighlightFreeAimPayload.Empty))
        {
            ClearRenderers();
            return;
        }

        List<TrajectoryArc> arcs = payload.Arcs ?? new();

        while (_arcRenderers.Count < arcs.Count)
            _arcRenderers.Add(Instantiate(_trajectoryRenderer));
        while (_arcRenderers.Count > arcs.Count)
        {
            Destroy(_arcRenderers[^1].gameObject);
            _arcRenderers.RemoveAt(_arcRenderers.Count - 1);
        }

        for (int i = 0; i < arcs.Count; i++)
            _arcRenderers[i].HighlightTrajectory(arcs[i].Start, arcs[i].End, arcs[i].PeakHeight, Color.white);
    }

    private void ClearRenderers()
    {
        foreach (TrajectoryRenderer renderer in _arcRenderers)
            Destroy(renderer.gameObject);
        _arcRenderers.Clear();
    }
}
