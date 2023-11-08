using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace FortnitePorting.Exports;

public class ExportMesh
{
    public string MeshPath;
    public int NumLods;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
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

    [JsonIgnore] public EFortCustomGender GenderPermitted;

    public void ProcessPoses(USkeletalMesh? skeletalMesh, UPoseAsset? poseAsset)
    {
        if (skeletalMesh is null || poseAsset?.PoseContainer is null) return;

        PoseNames = poseAsset.PoseContainer.GetPoseNames().ToArray();

        var skelMeshPath = GetFolder(skeletalMesh);
        var poseAssetPath = GetFolder(poseAsset);
        var folderAssets = AppVM.CUE4ParseVM.Provider.Files.Values.Where(file => file.Path.StartsWith(skelMeshPath, StringComparison.OrdinalIgnoreCase) || file.Path.StartsWith(poseAssetPath, StringComparison.OrdinalIgnoreCase));
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

    /*private const string METAHUMAN_POSEASSET =
        "FortniteGame/Content/Characters/Player/Male/Medium/Heads/M_MED_Jonesy3L_Head/Meshes/3L/3L_lod2_Facial_Poses_PoseAsset";
    private const string METAHUMAN_POSES =
        "FortniteGame/Content/Characters/Player/Male/Medium/Heads/M_MED_Jonesy3L_Head/Meshes/3L/3L_lod2_Facial_Poses";

    public void ProcessMetahumanPoses()
    {
        // this will definitely cause issues in the future
        // for metahuman faces
        if (!AppVM.CUE4ParseVM.Provider.TryLoadObject(METAHUMAN_POSEASSET, out UPoseAsset poseAsset)) return;
        PoseNames = poseAsset.PoseContainer.GetPoseNames().ToArray();
        
        if (!AppVM.CUE4ParseVM.Provider.TryLoadObject(METAHUMAN_POSES, out UAnimSequence animSequence)) return;
        var sequencePath = animSequence.GetPathName();
        PoseAnimation = sequencePath;
        ExportHelpers.Save(animSequence);
    }*/

    private string GetFolder(UObject obj)
    {
        return AppVM.CUE4ParseVM.Provider.FixPath(obj.GetPathName()).SubstringBeforeLast("/");
    }
}

public record ExportMaterial
{
    public string MaterialPath;
    public string MaterialName;
    public string? MasterMaterialName;
    public int SlotIndex;
    public int Hash;
    public bool IsGlass;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
    public List<SwitchParameter> Switches = new();
    public List<ComponentMaskParameter> ComponentMasks = new();
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
    public List<SwitchParameter> Switches = new();
    public List<ComponentMaskParameter> ComponentMasks = new();
}

public record TextureParameter(string Name, string Value, bool sRGB, TextureCompressionSettings CompressionSettings);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value)
{
    public FLinearColor Value { get; set; } = Value;
}

public record SwitchParameter(string Name, bool Value);

public record ComponentMaskParameter(string Name, FLinearColor Value);

public record TransformParameter(string Name, FTransform Value);

public class AnimationData
{
    public string Skeleton;
    public List<EmoteSection> Sections = new();
    public List<EmoteProp> Props = new();
    public List<ExportSound> Sounds = new();
}

public record EmoteSection(string Path, string Name, float Time, float Length, bool Loop = false)
{
    public string AdditivePath;
    public List<Curve> Curves = new();
}

public class Sound
{
    public USoundWave? SoundWave;
    public float Time;
    public bool Loop;

    public Sound(USoundWave? soundWave, float time, bool loop)
    {
        SoundWave = soundWave;
        Time = time;
        Loop = loop;
    }

    public ExportSound ToExportSound()
    {
        ExportHelpers.SaveSoundWave(SoundWave, out var audioFormat, out _);
        return new ExportSound(SoundWave.GetPathName(), audioFormat, Time, Loop);
    }

    public bool IsValid()
    {
        return SoundWave is not null && Time >= 0;
    }
}

public record ExportSound(string Path, string AudioExtension, float Time, bool Loop);

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