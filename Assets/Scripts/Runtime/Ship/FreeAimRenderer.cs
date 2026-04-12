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

    public void HighlightTargets(HighlightFreeAimPayload payload)
    {
        if (payload.Equals(HighlightFreeAimPayload.Empty))
        {
            ClearRenderers();
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
            {
                _activeMouseRenderer = Instantiate(_trajectoryRenderer);
            }
            Debug.Log($"{payload.MousePosition}");
            _activeMouseRenderer.HighlightTrajectory(payload.Origin.Transform.position, payload.MousePosition.Value, lineColor, payload.TrajectoryConfigData ?? _defaultTajectoryConfigData);
        }
    }

    private void ClearRenderers()
    {
        foreach (TrajectoryRenderer renderer in _activeRenderers.Values)
        {
            Destroy(renderer.gameObject);
        }

        if (_activeMouseRenderer != null) Destroy(_activeMouseRenderer.gameObject);

        _activeRenderers.Clear();
    }
}
