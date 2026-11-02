using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Rendering.Cache;
using System.Collections.Concurrent;
using FortnitePorting.Rendering.Materials;
using RenderMaterial = FortnitePorting.Rendering.Materials.Material;

namespace FortnitePorting.Rendering.Cache;

public static class MaterialCache
{
    private static readonly ConcurrentDictionary<string, RenderMaterial> Cache = new();

    public static RenderMaterial GetOrCreate(UMaterialInterface? materialInterface)
    {
        if (materialInterface is null)
            return new RenderMaterial();

        var key = materialInterface.GetPathName();
        
        if (Cache.TryGetValue(key, out var cachedMaterial))
            return cachedMaterial;

        var material = materialInterface switch
        {
            UMaterialInstanceConstant materialInstance => new RenderMaterial(materialInstance),
            UMaterial baseMaterial => new RenderMaterial(baseMaterial),
            _ => new RenderMaterial()
        };

        Cache.TryAdd(key, material);
        return material;
    }
    
    public static RenderMaterial GetOrCreateWithTextureData(UMaterialInterface? materialInterface, List<KeyValuePair<UBuildingTextureData, int>> textureData)
    {
        if (materialInterface is null)
            return new RenderMaterial();
        
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
            UMaterialInstanceConstant materialInstance => new RenderMaterial(materialInstance),
            UMaterial mat => new RenderMaterial(mat),
            _ => new RenderMaterial()
        };

        ApplyTextureDataToMaterial(ref material, textureData);

        Cache.TryAdd(key, material);
        return material;
    }

    private static void ApplyTextureDataToMaterial(ref RenderMaterial material, List<KeyValuePair<UBuildingTextureData, int>> textureData)
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

    private static string GenerateTextureDataKey(UMaterialInterface materialInterface, List<KeyValuePair<UBuildingTextureData, int>> textureData)
    {
        var basePath = materialInterface?.GetPathName() ?? "Invalid";
        
        var overrideData = textureData.FirstOrDefault(td => td.Key.OverrideMaterial.IsNull);
        if (overrideData.Key?.OverrideMaterial.TryLoad(out var overrideMaterial) ?? false)
        {
            basePath = overrideMaterial.GetPathName();
        }
        
        var textureDataKey = string.Join("|", textureData.Select(kvp => 
            $"{kvp.Key.GetPathName()}:{kvp.Value}"));
        
        return $"{basePath}_{textureDataKey}";
    }
}