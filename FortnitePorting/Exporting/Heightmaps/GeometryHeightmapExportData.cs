using System.Collections.Generic;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed class GeometryHeightmapExportData
{
    [JsonProperty("min_x")] public float MinX { get; init; }
    [JsonProperty("max_x")] public float MaxX { get; init; }
    [JsonProperty("min_y")] public float MinY { get; init; }
    [JsonProperty("max_y")] public float MaxY { get; init; }
    [JsonProperty("min_z")] public float MinZ { get; init; }
    [JsonProperty("max_z")] public float MaxZ { get; init; }
    [JsonProperty("metre_16bit")] public ushort Metre16Bit { get; init; }
    [JsonProperty("metre_px")] public float MetrePx { get; init; }
    [JsonProperty("total_meshes")] public int TotalMeshes { get; init; }

    [JsonIgnore] public int OutputWidth { get; init; }
    [JsonIgnore] public int OutputHeight { get; init; }
    [JsonIgnore] public bool TerrainMapSaved { get; init; }
    [JsonIgnore] public bool UsedTerrainBounds { get; init; }
    [JsonIgnore] public int RasterizedMeshes { get; init; }
    [JsonIgnore] public int TerrainMeshes { get; init; }
    [JsonIgnore] public int ClippedMeshes { get; init; }
    [JsonIgnore] public bool CroppedToMainComponent { get; init; }
    [JsonIgnore] public List<string> Warnings { get; init; } = [];
}

