using System;
using System.Collections.Generic;

public static class EnumerableExtensions
{
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> fn)
    {
        foreach (var item in enumerable)
        {
            fn(item);
            yield return item;
        }
    }
}
