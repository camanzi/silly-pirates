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
    {
        _lineRenderer.enabled = true;

        _lineRenderer.positionCount = _resolution;
        _lineRenderer.SetPosition(0, startPos);

        Vector3 midPoint = (startPos + endPos) / 2;
        Vector3 controlPoint = midPoint + Vector3.up * trajectoryConfig.Height;

        for (int i = 0; i < _resolution; i++) {
            float t = i / (float)(_resolution - 1);
            Vector3 pos = MathUtils.EvaluateBezierPoint(t, startPos, controlPoint, endPos);
            _lineRenderer.SetPosition(i, pos);
        }

        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
    }
}
