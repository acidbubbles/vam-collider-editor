public static class JSONStorableFloatExtensions
{
    public static JSONStorableFloat WithDefault(this JSONStorableFloat jsf, float defaultVal)
    {
        jsf.defaultVal = defaultVal;
        return jsf;
    }
}
