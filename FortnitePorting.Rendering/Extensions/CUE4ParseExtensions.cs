using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Rendering.Extensions;

public static class CUE4ParseExtensions
{
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
    
    public static CStaticMesh Convert(this UStaticMesh staticMesh)
    {
        staticMesh.TryConvert(out var convertedMesh);
        return convertedMesh;
    }
    
    public static CSkeletalMesh Convert(this USkeletalMesh skeletalMesh)
    {
        skeletalMesh.TryConvert(out var convertedMesh);
        return convertedMesh;
    }
}