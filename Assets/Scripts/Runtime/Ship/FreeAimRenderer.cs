using UnityEngine;

public class FreeAimRenderer : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    void OnEnable()
    {
        _lineRenderer.enabled = false;
    }

    public void HighlightTargets(HighlightPayload payload)
    {
        if (payload.targets.Count == 0)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;
        
        _lineRenderer.positionCount = payload.targets.Count + 1;
        _lineRenderer.SetPosition(0, payload.origin.Value);

        for (int i = 0; i < payload.targets.Count; i++)
        {
            _lineRenderer.SetPosition(i + 1, payload.targets[i]);
        }

        Color lineColor = payload.isValidHighlight ? Color.green : Color.red;
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
    }
}
