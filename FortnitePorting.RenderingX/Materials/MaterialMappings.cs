using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.RenderingX.Cache;
using FortnitePorting.RenderingX.Data.Textures;

namespace FortnitePorting.RenderingX.Materials;

public class MaterialMappings(string[] names)
{
    public string[] Names = names;

    public bool TrySetTexture(ref Texture2D? targetTexture, FTextureParameterValue parameter)
    {
        if (targetTexture is not null) return false;
        if (!Names.Contains(parameter.Name)) return false;
        if (parameter.ParameterValue.Load<UTexture2D>() is not { } texture) return false;

        targetTexture = TextureCache.GetOrCreate(texture);
        
        return targetTexture is not null;
    }
}