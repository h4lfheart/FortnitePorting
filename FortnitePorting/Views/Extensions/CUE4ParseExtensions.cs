using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Views.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetOrDefault<T>(this UObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.Properties.Any(x => x.Name.PlainText.Equals(name)))
            {
                return obj.GetOrDefault<T>(name);
            }
        }

        return default;
    }
}