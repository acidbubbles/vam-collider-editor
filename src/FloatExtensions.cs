using UnityEngine;

public static class FloatExtensions
{
    public static float ExponentialScale(this float inputValue, float midValue, float maxValue)
    {
        var m = maxValue / midValue;
        var c = Mathf.Log(Mathf.Pow(m - 1, 2));
        var b = maxValue / (Mathf.Exp(c) - 1);
        var a = -1 * b;
        return a + b * Mathf.Exp(c * inputValue);
    }
}
