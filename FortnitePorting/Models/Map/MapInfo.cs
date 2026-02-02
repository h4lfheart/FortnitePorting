
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Map;

public partial class MapInfo() : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _name;
    [ObservableProperty] private int _priority = 1;
    [ObservableProperty] private string _mapPath;
    [ObservableProperty] private string? _minimapPath;
    [ObservableProperty] private string? _maskPath;
    [ObservableProperty] private float _scale;
    [ObservableProperty] private int _xOffset;
    [ObservableProperty] private int _yOffset;
    [ObservableProperty] private int _minGridDistance;
    [ObservableProperty] private bool _useMask;
    [ObservableProperty] private bool _rotateGrid = true;
    [ObservableProperty] private bool _isNonDisplay = false;

    [JsonIgnore] public bool IsPublished = false;

    public MapInfo(
        string name,
        string mapPath,
        string? minimapPath = null,
        string? maskPath = null,
        float scale = 1,
        int xOffset = 0,
        int yOffset = 0,
        int minGridDistance = 12800,
        bool useMask = false,
        bool rotateGrid = true,
        bool isNonDisplay = false) : this()
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        MapPath = mapPath;
        MinimapPath = minimapPath;
        MaskPath = maskPath;
        Scale = scale;
        XOffset = xOffset;
        YOffset = yOffset;
        MinGridDistance = minGridDistance;
        UseMask = useMask;
        RotateGrid = rotateGrid;
        IsNonDisplay = isNonDisplay;
    }

    public static MapInfo CreateNonDisplay(string id, string mapPath)
    {
        return new MapInfo(
            id,
            mapPath,
            isNonDisplay: true
        );
    }

    public bool IsValid()
    {
        var isValid = true;
        
        isValid &= UEParse.Provider.Files.ContainsKey(
            UEParse.Provider.FixPath(MapPath + ".umap")
        );

        if (!IsNonDisplay)
        {
            isValid &= UEParse.Provider.Files.ContainsKey(
                UEParse.Provider.FixPath(MinimapPath + ".uasset")
            );

            if (UseMask)
            {
                isValid &= UEParse.Provider.Files.ContainsKey(
                    UEParse.Provider.FixPath(MaskPath + ".uasset")
                );
            }
        }
        
        return isValid;
    }
}