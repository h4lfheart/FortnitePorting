using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;

namespace FortnitePorting.Models.Fortnite;

public class UBuildingTextureData : UObject
{
    public UTexture2D? Diffuse;
    public UTexture2D? Normal;
    public UTexture2D? Specular;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Diffuse = GetOrDefault<UTexture2D>(nameof(Diffuse));
        Normal = GetOrDefault<UTexture2D>(nameof(Normal));
        Specular = GetOrDefault<UTexture2D>(nameof(Specular));
    }
}