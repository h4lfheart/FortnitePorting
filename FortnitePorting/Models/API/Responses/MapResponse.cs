using System;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class MapResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("map_path")]
    public string MapPath { get; set; }

    [JsonProperty("minmap_path")]
    public string? MinimapPath { get; set; }

    [JsonProperty("mask_path")]
    public string? MaskPath { get; set; }

    [JsonProperty("scale")]
    public float Scale { get; set; }

    [JsonProperty("x_offset")]
    public int XOffset { get; set; }

    [JsonProperty("y_offset")]
    public int YOffset { get; set; }

    [JsonProperty("cell_size")]
    public int CellSize { get; set; }
    
    [JsonProperty("min_grid_dist")]
    public int MinGridDistance { get; set; }

    [JsonProperty("use_mask")]
    public bool UseMask { get; set; }

    [JsonProperty("rotate_grid")]
    public bool RotateGrid { get; set; }

    [JsonProperty("is_non_display")]
    public bool IsNonDisplay { get; set; }
}