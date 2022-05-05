using System.Collections.Generic;

public static class Filters
{
    public const string None = "";
    public const string ModifiedOnly = "Modified Only";
    public const string NotModifiedOnly = "Not Modified Only";

    public static readonly List<string> List = new List<string>
    {
        None,
        ModifiedOnly,
        NotModifiedOnly
    };
}
