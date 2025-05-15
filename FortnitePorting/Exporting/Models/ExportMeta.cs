using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Models.Fortnite;

namespace FortnitePorting.Exporting.Models;

public class BaseMeta
{
    
}

public class ExportMasterSkeletonMeta : BaseMeta
{
    public ExportMesh MasterSkeletalMesh;
}

public class ExportPoseAssetMeta : BaseMeta
{
    public string PoseAsset;
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

public class ExportHeadMeta : ExportPoseAssetMeta
{
    public readonly Dictionary<ECustomHatType, string> MorphNames = new();
    public FLinearColor SkinColor;
}