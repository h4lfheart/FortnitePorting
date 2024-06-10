using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Export.Models;

public class BaseMeta
{
    
}

public class ExportPoseDataMeta : BaseMeta
{
    public List<PoseData> PoseData = [];
    public List<ReferencePose> ReferencePose = [];
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

public class ExportHeadMeta : ExportPoseDataMeta
{
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