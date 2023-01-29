using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace FortnitePorting.Exports;

public class ExportMesh
{
    public string MeshPath;
    public int NumLods;
    public FVector Offset = FVector.ZeroVector;
    public FVector Scale = FVector.OneVector;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
}

public class ExportMeshOverride : ExportMesh
{
    public string MeshToSwap;
}

public class ExportPart : ExportMesh
{
    public string Part;
    public string? MorphName;
    public string? SocketName;
    public string? PoseAnimation;
    public string[]? PoseNames;

    [JsonIgnore]
    public EFortCustomGender GenderPermitted;

    public void ProcessPoses(USkeletalMesh? skeletalMesh, UPoseAsset? poseAsset)
    {
        if (skeletalMesh is null || poseAsset is null) return;
        
        PoseNames = poseAsset.PoseContainer.PoseNames.Select(x => x.DisplayName.Text).ToArray();
                            
        var folderPath = AppVM.CUE4ParseVM.Provider.FixPath(skeletalMesh.GetPathName()).SubstringBeforeLast("/");
        var folderAssets = AppVM.CUE4ParseVM.Provider.Files.Values.Where(file => file.Path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase));
        foreach (var asset in folderAssets)
        {
            if (!AppVM.CUE4ParseVM.Provider.TryLoadObject(asset.PathWithoutExtension, out UAnimSequence animSequence)) continue;
            if (animSequence.Name.Contains("Hand_Cull", StringComparison.OrdinalIgnoreCase)) continue;
            if (animSequence.Name.Contains("FaceBakePose", StringComparison.OrdinalIgnoreCase)) continue;

            var sequencePath = animSequence.GetPathName();
            PoseAnimation = sequencePath;
            ExportHelpers.Save(animSequence);
            break;
        }
    }
}

public record ExportMaterial
{
    public string MaterialName;
    public string? MasterMaterialName;
    public int SlotIndex;
    public int Hash;
    public bool IsGlass;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
}

public record ExportMaterialOverride : ExportMaterial
{
    public string? MaterialNameToSwap;
}

public record ExportMaterialParams
{
    public string MaterialToAlter;
    public int Hash;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
}

public record TextureParameter(string Name, string Value, bool sRGB);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value)
{
    public FLinearColor Value { get; set; } = Value;
}

public record TransformParameter(string Name, FTransform Value);

public class AnimationData
{
    public string Skeleton;
    public List<EmoteSection> Sections = new();
    public List<EmoteProp> Props = new();
    public List<EmoteSound> Sounds = new();
}

public record EmoteSection(string Path, string Name, float Time, float Length, bool Loop = false)
{
    public string AdditivePath;
    public List<Curve> Curves = new();
}
public record EmoteSound(string Path, string AudioExtension, float Time, bool Loop);

public class EmoteProp
{
    public string SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
    public ExportMesh? Prop;
    public string Animation;
}

public class Curve
{
    public string Name;
    public List<CurveKey> Keys;
}
public record CurveKey(float Time, float Value);
