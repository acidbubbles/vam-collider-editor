using UnityEngine;

public static class ColorExtensions
{
    public static Color ToColor(this string value)
    {
        Color color;
        ColorUtility.TryParseHtmlString(value, out color);
        color.a = 0.005f;
        return color;
    }
}
