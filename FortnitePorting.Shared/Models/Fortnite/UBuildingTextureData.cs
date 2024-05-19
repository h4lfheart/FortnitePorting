using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using FortnitePorting.Shared.Models.CUE4Parse;

namespace FortnitePorting.Shared.Models.Fortnite;

public class UBuildingTextureData : UCustomObject
{
    public UTexture2D Diffuse;
    public UTexture2D Normal;
    public UTexture2D Specular;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Diffuse = GetOrDefault<UTexture2D>(nameof(Diffuse));
        Normal = GetOrDefault<UTexture2D>(nameof(Normal));
        Specular = GetOrDefault<UTexture2D>(nameof(Specular));
    }
}