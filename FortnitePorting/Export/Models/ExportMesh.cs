using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ExportMesh
{
    public string Name = string.Empty;
    public string Path = string.Empty;
    public int NumLods;
    
    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
}

public record ExportPart : ExportMesh
{
    [JsonIgnore] public EFortCustomGender GenderPermitted;
    
    public EFortCustomPartType Type;
    public BaseMeta Meta = new();
}

public class BaseMeta
{
    
}

public class ExportAttachMeta : BaseMeta
{
    public bool AttachToSocket;
    public string? Socket;
}

public class ExportHatMeta : ExportAttachMeta
{
    public string HatType;
}

public class ExportHeadMeta : BaseMeta
{
    public List<PoseData> PoseData = [];
    public List<ReferencePose> ReferencePose = [];
    public readonly Dictionary<ECustomHatType, string> MorphNames = new();
    public FLinearColor SkinColor;
}

public class PoseData
{
    public string Name;
    public List<PoseKey> Keys = [];
    public readonly float[] CurveData;

    public PoseData(string name, float[] curveData)
    {
        Name = name;
        CurveData = curveData;
    }
}

public record ReferencePose(string BoneName, FVector Location, FQuat Rotation, FVector Scale);

public record PoseKey(string Name, FVector Location, FQuat Rotation, FVector Scale, int PoseIndex, int BoneTransformIndex);
