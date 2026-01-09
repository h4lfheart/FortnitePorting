using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Models.Fortnite;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    extension(UObject asset)
    {
        public bool TryLoadEditorData<T>(out T? editorData) where T : UObject
        {
            try
            {
                var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
                if (UEParse.Provider.TryLoadObjectExports(path, out var exports))
                {
                    editorData = exports.FirstOrDefault(export => export.GetType() == typeof(T)) as T;
                    return editorData is not null;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        
            editorData = null;
            return false;
        }
        
        public string GetCleanedExportPath()
        {
            var path = asset.Owner != null ? asset.Owner.Name : string.Empty;
            path = path.SubstringBeforeLast('.');
            if (path.StartsWith('/')) path = path[1..];
        
            return path;
        }

        public T? GetAnyOrDefault<T>(params string[] names)
        {
            foreach (var name in names)
                if (asset.Properties.Any(x => x.Name.Text.Equals(name)))
                    return asset.GetOrDefault<T>(name);

            return default;
        }
        
        public FTransform GetAbsoluteTransformFromRootComponent()
        {
            var rootComponentLazy = asset.GetOrDefault<FPackageIndex?>("RootComponent");
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
        
        public T? GetVehicleMetadata<T>(params string[] names) where T : class
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
        
        public Bitmap? GetEditorIconBitmap()
        {
            var typeName = asset switch
            {
                UBuildingTextureData => "DataAsset",
                _ => asset.GetType().Name[1..]
            };

            typeName = typeName.Replace("EditorOnlyData", string.Empty);
        
            var filePath = $"avares://FortnitePorting/Assets/Unreal/{typeName}_64x.png";
            return !AssetLoader.Exists(new Uri(filePath)) ? null : ImageExtensions.AvaresBitmap(filePath);
        }
        
    }
    
    extension<T>(T obj) where T : UObject
    {
        public void GatherTemplateProperties()
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
    }

    extension(FStructFallback fallback)
    {
        public T? GetAnyOrDefault<T>(params string[] names)
        {
            foreach (var name in names)
                if (fallback.Properties.Any(x => x.Name.Text.Equals(name)))
                    return fallback.GetOrDefault<T>(name);

            return default;
        }
    }

    extension(FGameplayTagContainer? tags)
    {
        public FName? GetValueOrDefault(string category, FName def = default)
        {
            return tags?.GameplayTags is { Length: > 0 } ? tags.Value.GetValue(category) : def;
        }

        public bool ContainsAny(params string[] check)
        {
            return tags is not null && check.Any(x => tags.ContainsAny(x));
        }

        public bool ContainsAny(string check)
        {
            return tags is not null && tags.Value.Any(x => x.TagName.Text.Contains(check, StringComparison.OrdinalIgnoreCase));
        }
    }
    

    extension(TIntVector3<float> vec)
    {
        public FVector ToFVector()
        {
            return new FVector(vec.X, vec.Y, vec.Z);
        }
    }
    
    extension(FStaticComponentMaskParameter componentMask)
    {
        public FLinearColor ToLinearColor()
        {
            return new FLinearColor
            {
                R = componentMask.R ? 1 : 0,
                G = componentMask.G ? 1 : 0,
                B = componentMask.B ? 1 : 0,
                A = componentMask.A ? 1 : 0
            };
        }
    }
    
    extension(AbstractFileProvider provider)
    {
        public bool TryLoadObjectExports(string path, out IEnumerable<UObject> exports)
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

        public IEnumerable<UObject> LoadAllObjects(string path)
        {
            return provider.LoadPackage(path).GetExports();
        }

        public async Task<IEnumerable<UObject>> LoadAllObjectsAsync(string path)
        {
            var package = await provider.LoadPackageAsync(path);
            return package.GetExports();
        }
    }

    extension(FPackageIndex packageIndex)
    {
        public async Task<T?> LoadOrDefaultAsync<T>(T def = default) where T : UObject
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

        public T? LoadOrDefault<T>(T def = default) where T : UObject
        {
            return packageIndex.LoadOrDefaultAsync(def).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    extension(FSoftObjectPath packageIndex)
    {
        public async Task<T?> LoadOrDefaultAsync<T>(T def = default) where T : UObject
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

        public T? LoadOrDefault<T>(T def = default) where T : UObject
        {
            return Task.Run(async () => await packageIndex.LoadOrDefaultAsync(def)).Result;
        }
    }


    extension(IPropertyHolder propertyHolder)
    {
        public List<KeyValuePair<T, int>> GetAllProperties<T>(string name)
        {
            var propertyTags = new List<FPropertyTag>();
            foreach (var property in propertyHolder.Properties)
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
        
        public T? GetDataListItem<T>(params string[] names)
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

        public List<T> GetDataListItems<T>(params string[] names)
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

    extension(FColor color)
    {
        public FLinearColor ToLinearColor()
        {
            return new FLinearColor(
                MathF.Pow((float) color.R / byte.MaxValue, 2.2f), 
                MathF.Pow((float) color.G / byte.MaxValue, 2.2f), 
                MathF.Pow((float) color.B / byte.MaxValue, 2.2f), 
                MathF.Pow((float) color.A / byte.MaxValue, 2.2f));
        }
    }
    
    extension(USceneComponent component)
    {
        public FTransform GetAbsoluteTransform()
        {
            var result = new FTransform(component.RelativeRotation, component.RelativeLocation, component.RelativeScale3D);
            for (var attachParent = GetAttachParent(component); attachParent != null; attachParent = attachParent.GetAttachParent())
            {
                result *= new FTransform(attachParent.RelativeRotation, attachParent.RelativeLocation, attachParent.RelativeScale3D);
            }
            return result;
        }

        public USceneComponent? GetAttachParent()
        {
            return component.GetOrDefault("AttachParent", new FPackageIndex()).Load<USceneComponent>();
        }
    }
}