using UnityEngine;

public static class MathUtils
{
    public static Vector3 EvaluateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2) 
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }
}
