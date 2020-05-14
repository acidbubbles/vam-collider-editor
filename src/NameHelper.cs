public static class NameHelper
{
    public static string Simplify(string label)
    {
        if (label.StartsWith("AutoColliderAutoColliders"))
            return label.Substring("AutoColliderAutoColliders".Length);
        if (label.StartsWith("AutoColliderFemaleAutoColliders"))
            return label.Substring("AutoColliderFemaleAutoColliders".Length);
        if (label.StartsWith("AutoCollider"))
            return label.Substring("AutoCollider".Length);
        if (label.StartsWith("FemaleAutoColliders"))
            return label.Substring("FemaleAutoColliders".Length);
        if (label.StartsWith("_"))
            return label.Substring(1);
        return label;
    }
}
