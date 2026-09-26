using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Rendering.Data.Textures;

namespace FortnitePorting.Rendering.Materials;

public class LayeredMaterialMappings
{
    public MaterialMappings[] Layers = [];
    
    public bool TrySetTexture(Texture2D?[] targetTextures, FTextureParameterValue parameter)
    {
        var setTexture = false;
        for (var layerIndex = 0; layerIndex < Math.Min(Layers.Length, targetTextures.Length); layerIndex++)
        {
            setTexture |= Layers[layerIndex].TrySetTexture(ref targetTextures[layerIndex], parameter);
        }

        return setTexture;
    }
}