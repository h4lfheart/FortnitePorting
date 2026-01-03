using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
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
        if (texture.Decode() is not { } decodedTexture) return false;
        if (decodedTexture.ToSkBitmap() is not { } bitmap) return false;
        
        var tex = new Texture2D(bitmap.Width, bitmap.Height, bitmap.Bytes);
        tex.Generate();
        
        targetTexture = tex;
        
        bitmap.Dispose();
        return true;
    }
}