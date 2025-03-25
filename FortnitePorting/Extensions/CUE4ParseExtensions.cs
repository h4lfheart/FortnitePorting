using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using CUE4Parse.Utils;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    
    public static bool TryLoadEditorData<T>(this UObject asset, out T? editorData) where T : UObject
    {
        var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
        if (CUE4ParseVM.Provider.TryLoadObjectExports(path, out var exports))
        {
            editorData = exports.FirstOrDefault() as T;
            return editorData is not null;
        }

        editorData = default;
        return false;
    }
    
    public static string GetCleanedExportPath(UObject obj)
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];
        
        return path;
    }
    
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
        return tags.Value.Any(x => x.TagName.Text.Contains(check, StringComparison.OrdinalIgnoreCase));
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
    
    public static bool TryLoadObjectExports(this AbstractFileProvider provider, string path, out IEnumerable<UObject> exports)
    {
        exports = [];
        try
        {
            exports = provider.LoadPackage(path).GetExports();
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
    
    public static IEnumerable<UObject> LoadAllObjects(this AbstractFileProvider provider, string path)
    {
        return provider.LoadPackage(path).GetExports();
    }
    
    public static async Task<IEnumerable<UObject>> LoadAllObjectsAsync(this AbstractFileProvider provider, string path)
    {
        var package = await provider.LoadPackageAsync(path);
        return package.GetExports();
    }
    
    public static async Task<T?> LoadOrDefaultAsync<T>(this FPackageIndex packageIndex, T def = default) where T : UObject
    {
        try
        {
            return await packageIndex.LoadAsync<T>();
        }
        catch (Exception)
        {
            return def;
        }
    }
    
    public static T? LoadOrDefault<T>(this FPackageIndex packageIndex, T def = default) where T : UObject
    {
        return packageIndex.LoadOrDefaultAsync(def).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    
    public static async Task<T?> LoadOrDefaultAsync<T>(this FSoftObjectPath packageIndex, T def = default) where T : UObject
    {
        try
        {
            return await packageIndex.LoadAsync<T>();
        }
        catch (Exception)
        {
            return def;
        }
    }
    
    public static T? LoadOrDefault<T>(this FSoftObjectPath packageIndex, T def = default) where T : UObject
    {
        return Task.Run(async () => await packageIndex.LoadOrDefaultAsync(def)).Result;
    }

    public static UObject? TryGetFortComponentByType(this UObject obj, string componentType)
    {
        var componentContainer = obj.GetOrDefault<FStructFallback?>("ComponentContainer");
        var components = componentContainer?.Get<UObject[]>("Components");
        return components?.FirstOrDefault(component => component.ExportType.Equals(componentType, StringComparison.OrdinalIgnoreCase));
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
    
    public static T? GetVehicleMetadata<T>(this UObject asset, params string[] names) where T : class
    {
        FStructFallback? GetMarkerDisplay(UBlueprintGeneratedClass? blueprint)
        {
            var obj = blueprint?.ClassDefaultObject.Load();
            return obj?.GetOrDefault<FStructFallback>("MarkerDisplay");
        }

        var output = asset.GetAnyOrDefault<T?>(names);
        if (output is not null) return output;

        var vehicle = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
        output = GetMarkerDisplay(vehicle)?.GetAnyOrDefault<T?>(names);
        if (output is not null) return output;

        var vehicleSuper = vehicle.SuperStruct.Load<UBlueprintGeneratedClass>();
        output = GetMarkerDisplay(vehicleSuper)?.GetAnyOrDefault<T?>(names);
        return output;
    }

    public static FLinearColor ToLinearColor(this FColor color)
    {
        return new FLinearColor(
            MathF.Pow((float) color.R / byte.MaxValue, 2.2f), 
            MathF.Pow((float) color.G / byte.MaxValue, 2.2f), 
            MathF.Pow((float) color.B / byte.MaxValue, 2.2f), 
            MathF.Pow((float) color.A / byte.MaxValue, 2.2f));
    }

    public static void GatherTemplateProperties<T>(this T obj) where T : UObject
    {
        var current = obj;
        while (true)
        {
            current = current.Template?.Load<T>();
            if (current is null) break;

            foreach (var property in current.Properties)
            {
                if (obj.Properties.Any(prop => prop.Name.Text.Equals(property.Name.Text))) continue;
                
                obj.Properties.Add(property);
            }
            
            if (current.Template is null) break;
        }
        
        var fields = obj.GetType().GetFields();
        foreach (var field in fields)
        {
            if (field.DeclaringType == typeof(UObject)) continue;
            
            var targetProperty = obj.Properties.FirstOrDefault(prop => prop.Name.Text.Equals(field.Name));
            if (targetProperty is null) continue;
            
            field.SetValue(obj, targetProperty.Tag?.GetValue(field.FieldType));
        }
    }
    
    public static FTransform GetAbsoluteTransformFromRootComponent(this UObject actor)
    {
        var rootComponentLazy = actor.GetOrDefault<FPackageIndex?>("RootComponent");
        if (rootComponentLazy == null || rootComponentLazy.IsNull) return FTransform.Identity;

        var rootComponent = rootComponentLazy.Load();
        if (rootComponent is null) return FTransform.Identity;
        
        if (rootComponent is USceneComponent sceneComponent)
        {
            return sceneComponent.GetAbsoluteTransform();
        }
        
        var location = rootComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector);
        var rotation = rootComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
        var scale = rootComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);
        return new FTransform(rotation, location, scale);
    }
    
    public static FTransform GetAbsoluteTransform(this USceneComponent component)
    {
        var result = new FTransform(component.RelativeRotation, component.RelativeLocation, component.RelativeScale3D);
        for (var attachParent = GetAttachParent(component); attachParent != null; attachParent = attachParent.GetAttachParent())
        {
            result *= new FTransform(attachParent.RelativeRotation, attachParent.RelativeLocation, attachParent.RelativeScale3D);
        }
        return result;
    }
    
    public static USceneComponent? GetAttachParent(this USceneComponent component)
    {
        return component.GetOrDefault("AttachParent", new FPackageIndex()).Load<USceneComponent>();
    }
}