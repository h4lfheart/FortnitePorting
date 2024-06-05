using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Shared.Models.CUE4Parse;

namespace FortnitePorting.Models.Unreal;

// todo push to cue4parse main
public class UVirtualTextureBuilder : UCustomObject
{
    public FPackageIndex Texture;
    public int BuildHash;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Texture = GetOrDefault<FPackageIndex>(nameof(Texture));
        BuildHash = GetOrDefault<int>(nameof(BuildHash));
    }
}