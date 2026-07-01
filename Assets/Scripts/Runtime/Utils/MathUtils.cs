using UnityEngine;

public static class MathUtils
{
    public const float K = 2f;

    public static Vector3 EvaluateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }

    public static int CalculateHitChance(float accuracy, float evasion)
    {
        if (accuracy <= 0) return 0;
        if (evasion <= 0) return 100;
        float accPow = Mathf.Pow(accuracy, K);
        float evaPow = Mathf.Pow(evasion, K);
        return Mathf.RoundToInt(accPow / (accPow + evaPow) * 100f);
    }

    public static float CalculateOvercapBonus(int extraPoints)
    {
        if (extraPoints <= 0) return 0f;
        return Mathf.Pow(extraPoints + 1, 2) - (1.25f * extraPoints);
    }
}
