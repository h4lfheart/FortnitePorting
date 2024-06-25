using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

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

    public static List<KeyValuePair<T, int>> GetAllProperties<T>(this IPropertyHolder holder, string name)
    {
        var propertyTags = new List<FPropertyTag>();
        foreach (var property in holder.Properties)
        {
            if (property.Name.Text.Equals(name))
            {
                propertyTags.Add(property);
            }
        }

        var values = new List<KeyValuePair<T, int>>();
        foreach (var property in propertyTags)
        {
            var propertyValue = (T) property.Tag.GetValue(typeof(T));
            values.Add(new KeyValuePair<T, int>(propertyValue, property.ArrayIndex));
        }

        return values;
    }
    
    public static FLinearColor ToLinearColor(this FStaticComponentMaskParameter componentMask)
    {
        return new FLinearColor
        {
            R = componentMask.R ? 1 : 0,
            G = componentMask.G ? 1 : 0,
            B = componentMask.B ? 1 : 0,
            A = componentMask.A ? 1 : 0
        };
    }
    
    
    public static bool TryLoadEditorData<T>(this UObject asset, out T? editorData) where T : UObject
    {
        var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
        if (CUE4ParseVM.OptionalProvider.TryLoadObjectExports(path, out var exports))
        {
            editorData = exports.FirstOrDefault() as T;
            return editorData is not null;
        }

        editorData = default;
        return false;
    }
    
    public static bool TryLoadObjectExports(this AbstractFileProvider provider, string path, out IEnumerable<UObject> exports)
    {
        exports = Enumerable.Empty<UObject>();
        try
        {
            exports = provider.LoadAllObjects(path);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        catch (AggregateException e) // wtf
        {
            return false;
        }

        return true;
    }
    
    public static T? GetDataListItem<T>(this IPropertyHolder propertyHolder, params string[] names)
    {
        T? returnValue = default;
        if (propertyHolder.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
        {
            foreach (var data in dataList)
            {
                if (data.NonConstStruct is not null && data.NonConstStruct.TryGetValue(out returnValue, names)) break;
            }
        }

        return returnValue;
    }
    
    public static List<T> GetDataListItems<T>(this IPropertyHolder propertyHolder, params string[] names)
    {
        var returnList = new List<T>();
        if (propertyHolder.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
        {
            foreach (var data in dataList)
            {
                if (data.NonConstStruct is not null && data.NonConstStruct.TryGetValue(out T obj, names))
                {
                    returnList.Add(obj);
                }
            }
        }

        return returnList;
    }
}