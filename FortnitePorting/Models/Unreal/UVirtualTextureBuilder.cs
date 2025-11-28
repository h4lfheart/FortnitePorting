using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal;

public class UVirtualTextureBuilder : UObject
{
    [UProperty] public FPackageIndex Texture;
    [UProperty] public int BuildHash;
}