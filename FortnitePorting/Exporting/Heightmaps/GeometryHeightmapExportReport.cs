using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed class GeometryHeightmapExportReport
{
    [JsonProperty("generated_at")] public string GeneratedAt { get; init; } = DateTimeOffset.Now.ToString("O");
    [JsonProperty("map_name")] public string MapName { get; init; } = string.Empty;
    [JsonProperty("world_name")] public string WorldName { get; init; } = string.Empty;
    [JsonProperty("selected_maps_count")] public int SelectedMapsCount { get; init; }
    [JsonProperty("selected_maps")] public string[] SelectedMaps { get; init; } = [];
    [JsonProperty("include_main_level")] public bool IncludeMainLevel { get; init; }
    [JsonProperty("world_flags")] public string[] WorldFlags { get; init; } = [];
    [JsonProperty("resolution")] public int Resolution { get; init; }
    [JsonProperty("output_width")] public int OutputWidth { get; init; }
    [JsonProperty("output_height")] public int OutputHeight { get; init; }
    [JsonProperty("save_terrain_separately")] public bool SaveTerrainSeparately { get; init; }
    [JsonProperty("terrain_map_saved")] public bool TerrainMapSaved { get; init; }
    [JsonProperty("use_terrain_bounds")] public bool UseTerrainBounds { get; init; }
    [JsonProperty("used_terrain_bounds")] public bool UsedTerrainBounds { get; init; }
    [JsonProperty("include_spawn_island")] public bool IncludeSpawnIsland { get; init; }
    [JsonProperty("fill_resolution")] public bool FillResolution { get; init; }
    [JsonProperty("crop_to_main_component")] public bool CropToMainComponent { get; init; }
    [JsonProperty("cropped_to_main_component")] public bool CroppedToMainComponent { get; init; }
    [JsonProperty("source_meshes")] public int SourceMeshes { get; init; }
    [JsonProperty("rasterized_meshes")] public int RasterizedMeshes { get; init; }
    [JsonProperty("terrain_meshes")] public int TerrainMeshes { get; init; }
    [JsonProperty("clipped_meshes")] public int ClippedMeshes { get; init; }
    [JsonProperty("skipped_meshes")] public int SkippedMeshes { get; init; }
    [JsonProperty("filtered_meshes")] public int FilteredMeshes { get; init; }
    [JsonProperty("bounds")] public GeometryHeightmapReportBounds Bounds { get; init; } = new();
    [JsonProperty("scale")] public GeometryHeightmapReportScale Scale { get; init; } = new();
    [JsonProperty("filter_rules")] public Dictionary<string, int> FilterRules { get; init; } = [];
    [JsonProperty("filter_samples")] public GeometryHeightmapFilteredMeshSample[] FilterSamples { get; init; } = [];
    [JsonProperty("warnings")] public string[] Warnings { get; init; } = [];

    public static GeometryHeightmapExportReport Create(
        string mapName,
        string worldName,
        IReadOnlyCollection<string> selectedMaps,
        bool includeMainLevel,
        EWorldFlags worldFlags,
        GeometryHeightmapGenerationOptions options,
        GeometryHeightmapExportData exportData,
        int skippedMeshes,
        GeometryHeightmapFilterSummary filterSummary)
    {
        return new GeometryHeightmapExportReport
        {
            MapName = mapName,
            WorldName = worldName,
            SelectedMapsCount = selectedMaps.Count,
            SelectedMaps = selectedMaps.ToArray(),
            IncludeMainLevel = includeMainLevel,
            WorldFlags = GetWorldFlagNames(worldFlags),
            Resolution = options.Resolution,
            OutputWidth = exportData.OutputWidth,
            OutputHeight = exportData.OutputHeight,
            SaveTerrainSeparately = options.SaveTerrainSeparately,
            TerrainMapSaved = exportData.TerrainMapSaved,
            UseTerrainBounds = options.UseTerrainBounds,
            UsedTerrainBounds = exportData.UsedTerrainBounds,
            IncludeSpawnIsland = options.IncludeSpawnIsland,
            FillResolution = options.FillResolution,
            CropToMainComponent = options.CropToMainComponent,
            CroppedToMainComponent = exportData.CroppedToMainComponent,
            SourceMeshes = exportData.TotalMeshes,
            RasterizedMeshes = exportData.RasterizedMeshes,
            TerrainMeshes = exportData.TerrainMeshes,
            ClippedMeshes = exportData.ClippedMeshes,
            SkippedMeshes = skippedMeshes,
            FilteredMeshes = filterSummary.Total,
            Bounds = new GeometryHeightmapReportBounds(
                exportData.MinX,
                exportData.MaxX,
                exportData.MinY,
                exportData.MaxY,
                exportData.MinZ,
                exportData.MaxZ),
            Scale = new GeometryHeightmapReportScale(exportData.Metre16Bit, exportData.MetrePx),
            FilterRules = filterSummary.Rules.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value),
            FilterSamples = filterSummary.Samples.ToArray(),
            Warnings = exportData.Warnings.ToArray()
        };
    }

    private static string[] GetWorldFlagNames(EWorldFlags flags)
    {
        return Enum.GetValues<EWorldFlags>()
            .Where(flag => flags.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToArray();
    }
}

public sealed record GeometryHeightmapReportBounds(
    [property: JsonProperty("min_x")] float MinX = 0.0f,
    [property: JsonProperty("max_x")] float MaxX = 0.0f,
    [property: JsonProperty("min_y")] float MinY = 0.0f,
    [property: JsonProperty("max_y")] float MaxY = 0.0f,
    [property: JsonProperty("min_z")] float MinZ = 0.0f,
    [property: JsonProperty("max_z")] float MaxZ = 0.0f);

public sealed record GeometryHeightmapReportScale(
    [property: JsonProperty("metre_16bit")] ushort Metre16Bit = 0,
    [property: JsonProperty("metre_px")] float MetrePx = 0.0f);
