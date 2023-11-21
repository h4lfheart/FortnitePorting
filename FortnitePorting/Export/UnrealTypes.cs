using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;

namespace FortnitePorting.Export;

public enum EFortCustomPartType : byte
{
    Head = 0,
    Body = 1,
    Hat = 2,
    Backpack = 3,
    MiscOrTail = 4,
    Face = 5,
    Gameplay = 6,
    NumTypes = 7
}

public enum ECustomHatType : byte
{
    HeadReplacement,
    Cap,
    Mask,
    Helmet,
    Hat,
    None
}

public class URegisterThisUObject : UObject { }

public class UBuildingTextureData : URegisterThisUObject
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