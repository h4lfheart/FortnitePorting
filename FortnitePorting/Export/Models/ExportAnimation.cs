using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public class ExportAnimSection
{
    public string Path;
    public string Name;
    public float Time;
    public float Length;
    public float LinkValue;
    public bool Loop;
    public List<ExportCurve> Curves = [];

    [JsonIgnore] public UAnimSequence AssetRef;
}

public class ExportCurve
{
    public string Name;
    public List<ExportCurveKey> Keys;
}

public record ExportCurveKey(float Time, float Value);

public class ExportProp
{
    public ExportMesh Mesh;
    public List<ExportAnimSection> AnimSections;
    public string SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
}