using UnityEngine;

public class TrajectoryRenderer : MonoBehaviour
{
    [Header("Line Configs")]
    [SerializeField] private float _height = 5f;
    [SerializeField] private int _resolution = 20;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    public void HighlightTrajectory(Vector3 startPos, Vector3 endPos, Color lineColor)
    {
        _lineRenderer.enabled = true;

        _lineRenderer.positionCount = _resolution;
        _lineRenderer.SetPosition(0, startPos);

        Vector3 midPoint = (startPos + endPos) / 2;
        Vector3 controlPoint = midPoint + Vector3.up * _height;

        for (int i = 0; i < _resolution; i++) {
            float t = i / (float)(_resolution - 1);
            Vector3 pos = EvaluateBezierPoint(t, startPos, controlPoint, endPos);
            _lineRenderer.SetPosition(i, pos);
        }

        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
    }
    
    private Vector3 EvaluateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2) {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }
}
