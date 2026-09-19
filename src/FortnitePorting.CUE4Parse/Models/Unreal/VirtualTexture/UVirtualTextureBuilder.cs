using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.CUE4Parse.Models.Unreal.VirtualTexture;

public class UVirtualTextureBuilder : UObject
{
    [UProperty] public FPackageIndex Texture;
    [UProperty] public int BuildHash;
}