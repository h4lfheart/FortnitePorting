using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FortnitePorting.Models.Fortnite;

public class UBuildingTextureData : UObject
{
    [UProperty] public UTexture2D? Diffuse;
    [UProperty] public UTexture2D? Normal;
    [UProperty] public UTexture2D? Specular;
    [UProperty] public UMaterialInterface? OverrideMaterial;
}