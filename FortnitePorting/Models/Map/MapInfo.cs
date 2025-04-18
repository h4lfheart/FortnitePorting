namespace FortnitePorting.Models.Map;

public record MapInfo(string Name, string MapPath, string MinimapPath, string MaskPath, float Scale, int XOffset, int YOffset, int CellSize, int MinGridDistance, bool UseMask, bool RotateGrid = true, bool IsNonDisplay = false, string SourceName = "Battle Royale")
{
    public static MapInfo CreateNonDisplay(string name, string mapPath, string sourceName = "Battle Royale")
    {
        return new MapInfo(name, mapPath, null, null, 0, 0, 0, 0, 12800, false, true, true, sourceName);
    }
    
    public bool IsValid()
    {
        return CUE4ParseVM.Provider.Files.ContainsKey(CUE4ParseVM.Provider.FixPath(MapPath + ".umap"));
    }
}