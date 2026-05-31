namespace FortnitePorting.Exporting.Heightmaps;

public sealed record GeometryHeightmapGenerationOptions(
    int Resolution,
    bool SaveTerrainSeparately,
    GeometryHeightmapTrimSettings TrimSettings,
    bool IncludeSpawnIsland = false,
    bool FillResolution = true,
    bool CropToMainComponent = true)
{
    public const int HighResolutionWarningThreshold = 8192;
    public const int ExtremeResolutionWarningThreshold = 16384;
    public const int MaxExportResolution = 32768;
    public static readonly int[] ExportResolutions = [512, 1024, 2048, 4096, 8192, 16384, MaxExportResolution];
}

public sealed record GeometryHeightmapTrimSettings(
    float Top = 0.0f,
    float Right = 0.0f,
    float Bottom = 0.0f,
    float Left = 0.0f);

public sealed record GeometryHeightmapProgress(string Stage, int Current = 0, int Total = 0)
{
    public double Percentage => Total <= 0 ? 0.0 : Current * 100.0 / Total;
}

