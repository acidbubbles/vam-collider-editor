public static class JSONStorableExtensions
{
    public static JSONStorableFloat WithDefault(this JSONStorableFloat jsf, float defaultVal)
    {
        jsf.defaultVal = defaultVal;
        return jsf;
    }

    public static JSONStorableBool WithDefault(this JSONStorableBool jsb, bool defaultVal)
    {
        jsb.defaultVal = defaultVal;
        return jsb;
    }
}
