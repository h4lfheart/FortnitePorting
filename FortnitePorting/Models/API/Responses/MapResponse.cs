using System;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class MapResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Priority { get; set; }
    public string MapPath { get; set; }
    public string? MinimapPath { get; set; }
    public string? MaskPath { get; set; }
    public float Scale { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int MinGridDistance { get; set; }
    public bool UseMask { get; set; }
    public bool RotateGrid { get; set; }
    public bool IsNonDisplay { get; set; }
}