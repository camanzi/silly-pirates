using UnityEngine;

public class FreeAimRenderer : MonoBehaviour
{
    [Header("Shared line configs")]
    [SerializeField] private float _height = 5f;
    [SerializeField] private int _resolution = 20;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    void OnEnable()
    {
        _lineRenderer.enabled = false;
    }

    // FIXME later, attenzione PER OGNI target devo avere un line renderer dedicato per far partire piú lines.
    // Questo vuol dire predisporre anche un pooler
    public void HighlightTargets(HighlightFreeAimPayload payload)
    {
        if (payload.Equals(HighlightFreeAimPayload.Empty))
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;
        
        Color lineColor = payload.IsValidHighlight ? Color.green : Color.red;
        foreach (ITargettable target in payload.Targets)
        {
            DrawLine(payload.Origin.Transform.position, target.Transform.position, lineColor);
        }

        if (payload.MousePosition.HasValue)
        {
            DrawLine(payload.Origin.Transform.position, payload.MousePosition.Value, lineColor);
        }
    }

    private void DrawLine(Vector3 startPos, Vector3 endPos, Color lineColor)
    {
        // FIXME Pulla una nuova curva dal pooler e disegnala
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
