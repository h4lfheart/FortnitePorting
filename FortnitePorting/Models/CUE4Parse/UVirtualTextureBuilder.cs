using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.CUE4Parse;

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