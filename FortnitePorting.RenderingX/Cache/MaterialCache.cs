using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.RenderingX.Cache;
using FortnitePorting.RenderingX.Materials;
using System.Collections.Concurrent;

namespace FortnitePorting.RenderingX.Cache;

public static class MaterialCache
{
    private static readonly ConcurrentDictionary<string, Material> Cache = new();

    public static Material GetOrCreate(UMaterialInterface? materialInterface)
    {
        if (materialInterface is null)
            return new Material();

        var key = materialInterface.GetPathName();
        
        if (Cache.TryGetValue(key, out var cachedMaterial))
            return cachedMaterial;

        var material = materialInterface switch
        {
            UMaterialInstanceConstant materialInstance => new Material(materialInstance),
            UMaterial baseMaterial => new Material(baseMaterial),
            _ => new Material()
        };

        Cache.TryAdd(key, material);
        return material;
    }
    
    public static Material GetOrCreateWithTextureData(UMaterialInterface? materialInterface, List<KeyValuePair<UBuildingTextureData, int>> textureData)
    {
        if (textureData.Count == 0)
            return GetOrCreate(materialInterface);

        var key = GenerateTextureDataKey(materialInterface, textureData);
        
        if (Cache.TryGetValue(key, out var cachedMaterial))
            return cachedMaterial;

        var overrideData = textureData.FirstOrDefault(td => !td.Key.OverrideMaterial.IsNull);
        var baseMaterial = materialInterface;
        
        if (overrideData.Key?.OverrideMaterial.TryLoad(out UMaterialInterface overrideMaterial) ?? false)
        {
            baseMaterial = overrideMaterial;
        }

        var material = baseMaterial switch
        {
            UMaterialInstanceConstant materialInstance => new Material(materialInstance),
            UMaterial mat => new Material(mat),
            _ => new Material()
        };

        ApplyTextureDataToMaterial(ref material, textureData);

        Cache.TryAdd(key, material);
        return material;
    }

    private static void ApplyTextureDataToMaterial(ref Material material, List<KeyValuePair<UBuildingTextureData, int>> textureData)
    {
        foreach (var (buildingTextureData, layerIndex) in textureData)
        {
            if (buildingTextureData.Diffuse.TryLoad(out UTexture2D diffuseTexture))
                material.Diffuse[layerIndex] = TextureCache.GetOrCreate(diffuseTexture);
            
            if (buildingTextureData.Normal.TryLoad(out UTexture2D normalTexture))
                material.Normals[layerIndex] = TextureCache.GetOrCreate(normalTexture);
            
            if (buildingTextureData.Specular.TryLoad(out UTexture2D specularTexture))
                material.SpecularMasks[layerIndex] = TextureCache.GetOrCreate(specularTexture);
        }
    }

    private static string GenerateTextureDataKey(UMaterialInterface? materialInterface, List<KeyValuePair<UBuildingTextureData, int>> textureData)
    {
        var basePath = materialInterface?.GetPathName() ?? "null";
        
        var overrideData = textureData.FirstOrDefault(td => !td.Key.OverrideMaterial.IsNull);
        if (overrideData.Key?.OverrideMaterial.TryLoad(out var overrideMaterial) ?? false)
        {
            basePath = overrideMaterial.GetPathName();
        }
        
        var textureDataKey = string.Join("|", textureData.Select(kvp => 
            $"{kvp.Key.GetPathName()}:{kvp.Value}"));
        
        return $"{basePath}_{textureDataKey}";
    }
}