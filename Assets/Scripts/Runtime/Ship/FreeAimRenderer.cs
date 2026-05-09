using System.Collections.Generic;
using UnityEngine;

public class FreeAimRenderer : MonoBehaviour
{

    // FIXME Later da far diventare Poolable
    [Header("External References")]
    [SerializeField] private TrajectoryRenderer _trajectoryRenderer;
    [SerializeField] private TrajectoryConfigsSO _defaultTajectoryConfigData;

    private Dictionary<ITargettable, TrajectoryRenderer> _activeRenderers = new();
    private TrajectoryRenderer _activeMouseRenderer;
    private List<TrajectoryRenderer> _customArcRenderers = new();

    public void HighlightTargets(HighlightFreeAimPayload payload)
    {
        if (payload.Equals(HighlightFreeAimPayload.Empty))
        {
            ClearRenderers();
            return;
        }

        Color lineColor = Color.white;
        foreach (ITargettable target in payload.Targets ?? new())
        {
            if (_activeRenderers.ContainsKey(target)) continue;

            TrajectoryRenderer renderer = Instantiate(_trajectoryRenderer);
            _activeRenderers.Add(target, renderer);
            renderer.HighlightTrajectory(payload.Origin.Transform.position, target.Transform.position, lineColor, payload.TrajectoryConfigData ?? _defaultTajectoryConfigData);
        }

        if (payload.MousePosition.HasValue)
        {
            if (_activeMouseRenderer == null)
                _activeMouseRenderer = Instantiate(_trajectoryRenderer);
            _activeMouseRenderer.HighlightTrajectory(payload.Origin.Transform.position, payload.MousePosition.Value, lineColor, payload.TrajectoryConfigData ?? _defaultTajectoryConfigData);
        }

        if (payload.CustomArcs != null)
        {
            // Resize pool
            while (_customArcRenderers.Count < payload.CustomArcs.Count)
                _customArcRenderers.Add(Instantiate(_trajectoryRenderer));
            while (_customArcRenderers.Count > payload.CustomArcs.Count)
            {
                Destroy(_customArcRenderers[^1].gameObject);
                _customArcRenderers.RemoveAt(_customArcRenderers.Count - 1);
            }

            for (int i = 0; i < payload.CustomArcs.Count; i++)
            {
                TrajectoryArc arc = payload.CustomArcs[i];
                _customArcRenderers[i].HighlightTrajectory(arc.Start, arc.End, arc.PeakHeight, lineColor);
            }
        }
        else
        {
            ClearCustomArcRenderers();
        }
    }

    private void ClearRenderers()
    {
        foreach (TrajectoryRenderer renderer in _activeRenderers.Values)
            Destroy(renderer.gameObject);
        if (_activeMouseRenderer != null) Destroy(_activeMouseRenderer.gameObject);
        _activeRenderers.Clear();
        _activeMouseRenderer = null;
        ClearCustomArcRenderers();
    }

    private void ClearCustomArcRenderers()
    {
        foreach (var r in _customArcRenderers)
            Destroy(r.gameObject);
        _customArcRenderers.Clear();
    }
}
