namespace FortnitePorting.Exporting.Heightmaps;

public sealed class GeometryHeightmapGenerationResult
{
    public required string OutputFileName { get; init; }
    public int TotalMeshes { get; init; }
    public int OutputWidth { get; init; }
    public int OutputHeight { get; init; }
    public int RasterizedMeshes { get; init; }
}
