using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FortnitePorting.Extensions;

public static class ComparisonExtensions
{
    public static int CompareNatural(string x, string y)
    {
        var table = new Dictionary<string, string[]>();

        if (x == y)
            return 0;

        if (!table.TryGetValue(x, out var x1))
        {
            x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            table.Add(x, x1);
        }

        if (!table.TryGetValue(y, out var y1))
        {
            y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
            table.Add(y, y1);
        }

        int returnVal;

        for (var i = 0; i < x1.Length && i < y1.Length; i++)
            if (x1[i] != y1[i])
            {
                returnVal = PartCompare(x1[i], y1[i]);
                return returnVal;
            }

        if (y1.Length > x1.Length)
            returnVal = 1;
        else if (x1.Length > y1.Length)
            returnVal = -1;
        else
            returnVal = 0;

        return returnVal;
    }

    private static int PartCompare(string left, string right)
    {
        if (!int.TryParse(left, out var x))
            return left.CompareTo(right);

        if (!int.TryParse(right, out var y))
            return left.CompareTo(right);

        return x.CompareTo(y);
    }
}


public class CustomComparer<T>(Comparison<T> comparison) : IComparer<T>
{
    public int Compare(T x, T y) {
        return comparison(x, y);
    }
}