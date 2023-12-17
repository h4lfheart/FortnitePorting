using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

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

[StructFallback]
public class FStyleParameter<T>
{
    public T Value;
    public FName ParamName;
    public string Name => ParamName.Text;

    public FStyleParameter(FStructFallback fallback)
    {
        ParamName = fallback.GetOrDefault<FName>(nameof(ParamName));
        Value = fallback.GetOrDefault<T>(nameof(Value));
    }
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

public class UFortVehicleSkelMeshComponent : USkeletalMeshComponentBudgeted { }

public class FortAnimNotifyState_EmoteSound : URegisterThisUObject
{
    public USoundCue? EmoteSound1P { get; private set; }
    public USoundCue? EmoteSound3P { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EmoteSound1P = GetOrDefault<USoundCue>(nameof(EmoteSound1P));
        EmoteSound3P = GetOrDefault<USoundCue>(nameof(EmoteSound3P));
    }
}

public class FortAnimNotifyState_SpawnProp : URegisterThisUObject
{
    public FName SocketName { get; private set; }
    public FVector LocationOffset { get; private set; }
    public FRotator RotationOffset { get; private set; }
    public FVector Scale { get; private set; }
    public bool bInheritScale { get; private set; }
    public UStaticMesh? StaticMeshProp { get; private set; }
    public USkeletalMesh? SkeletalMeshProp { get; private set; }
    public UAnimSequence? SkeletalMeshPropAnimation { get; private set; }
    public UAnimMontage? SkeletalMeshPropMontage { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SocketName = GetOrDefault<FName>(nameof(SocketName));
        LocationOffset = GetOrDefault(nameof(LocationOffset), FVector.ZeroVector);
        RotationOffset = GetOrDefault(nameof(RotationOffset), FRotator.ZeroRotator);
        Scale = GetOrDefault(nameof(Scale), FVector.OneVector);
        bInheritScale = GetOrDefault<bool>(nameof(bInheritScale));
        StaticMeshProp = GetOrDefault<UStaticMesh>(nameof(StaticMeshProp));
        SkeletalMeshProp = GetOrDefault<USkeletalMesh>(nameof(SkeletalMeshProp));
        SkeletalMeshPropAnimation = GetOrDefault<UAnimSequence>(nameof(SkeletalMeshPropAnimation));
        SkeletalMeshPropMontage = GetOrDefault<UAnimMontage>(nameof(SkeletalMeshPropAnimation));
    }
}