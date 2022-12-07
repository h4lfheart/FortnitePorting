using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FortnitePorting.Views.Extensions;

public static class MiscExtensions
{
    public static bool MoveToEnd<T>(this List<T> list, Func<T, bool> predicate)
    {
        var found = list.FirstOrDefault(predicate);
        if (found is null) return false;

        var removed = list.Remove(found);
        if (!removed) return false;
        list.Add(found);

        return true;
    }
}