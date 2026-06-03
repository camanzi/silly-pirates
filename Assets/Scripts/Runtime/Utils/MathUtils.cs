using UnityEngine;

public static class MathUtils
{
    public static Vector3 EvaluateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }

    public static float CalculateOvercapBonus(int extraPoints)
    {
        if (extraPoints <= 0) return 0f;
        return Mathf.Pow(extraPoints + 1, 2) - (1.25f * extraPoints);
    }
}
