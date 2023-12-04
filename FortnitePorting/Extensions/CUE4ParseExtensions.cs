using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetAnyOrDefault<T>(this UObject obj, params string[] names)
    {
        foreach (var name in names)
            if (obj.Properties.Any(x => x.Name.Text.Equals(name)))
                return obj.GetOrDefault<T>(name);

        return default;
    }

    public static T GetAnyOrDefault<T>(this FStructFallback obj, params string[] names)
    {
        foreach (var name in names)
            if (obj.Properties.Any(x => x.Name.Text.Equals(name)))
                return obj.GetOrDefault<T>(name);

        return default;
    }

    public static FName? GetValueOrDefault(this FGameplayTagContainer tags, string category, FName def = default)
    {
        return tags.GameplayTags is { Length: > 0 } ? tags.GetValue(category) : def;
    }

    public static bool ContainsAny(this FGameplayTagContainer? tags, params string[] check)
    {
        if (tags is null) return false;
        return check.Any(x => tags.ContainsAny(x));
    }

    public static bool ContainsAny(this FGameplayTagContainer? tags, string check)
    {
        if (tags is null) return false;
        return tags.Value.Any(x => x.TagName.Text.Contains(check));
    }

    public static FVector ToFVector(this TIntVector3<float> vec)
    {
        return new FVector(vec.X, vec.Y, vec.Z);
    }
    
    public static bool TryGetDataTableRow(this Dictionary<FName, FStructFallback> dataTable, string rowKey, StringComparison comparisonType, out FStructFallback rowValue)
    {
        foreach (var kvp in dataTable)
        {
            if (kvp.Key.IsNone || !kvp.Key.Text.Equals(rowKey, comparisonType)) continue;

            rowValue = kvp.Value;
            return true;
        }

        rowValue = default;
        return false;
    }
}