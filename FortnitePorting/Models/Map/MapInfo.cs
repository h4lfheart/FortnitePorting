
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Map;

public partial class MapInfo() : ObservableObject
{
    [ObservableProperty] private string id;
    [ObservableProperty] private int _priority = 1;
    [ObservableProperty] private string mapPath;
    [ObservableProperty] private string? minimapPath;
    [ObservableProperty] private string? maskPath;
    [ObservableProperty] private float scale;
    [ObservableProperty] private int xOffset;
    [ObservableProperty] private int yOffset;
    [ObservableProperty] private int minGridDistance;
    [ObservableProperty] private bool useMask;
    [ObservableProperty] private bool rotateGrid = true;
    [ObservableProperty] private bool isNonDisplay = false;

    [JsonIgnore] public bool IsPublished = false;

    public MapInfo(
        string id,
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
        Id = id;
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