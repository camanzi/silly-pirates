using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [Header("Line Configs")]
    [SerializeField] private int _resolution = 20;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    public void HighlightTrajectory(Vector3 startPos, Vector3 endPos, Color lineColor, TrajectoryConfigsSO trajectoryConfig)
        => HighlightTrajectory(startPos, endPos, trajectoryConfig.Height, lineColor);

    public void HighlightTrajectory(Vector3 startPos, Vector3 endPos, float peakHeight, Color lineColor)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = _resolution;

        Vector3 midPoint = (startPos + endPos) / 2;
        Vector3 controlPoint = midPoint + Vector3.up * peakHeight;

        for (int i = 0; i < _resolution; i++)
        {
            float t = i / (float)(_resolution - 1);
            _lineRenderer.SetPosition(i, MathUtils.EvaluateBezierPoint(t, startPos, controlPoint, endPos));
        }

        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
    }
}
