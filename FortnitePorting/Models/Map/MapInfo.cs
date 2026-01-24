namespace FortnitePorting.Models.Map;

public record MapInfo(string Id, string MapPath, string MinimapPath, string MaskPath, float Scale, int XOffset, int YOffset, int CellSize, int MinGridDistance, bool UseMask, bool RotateGrid = true, bool IsNonDisplay = false, string SourceName = "Battle Royale")
{
    public static MapInfo CreateNonDisplay(string id, string mapPath, string sourceName = "Battle Royale")
    {
        return new MapInfo(id, mapPath, null, null, 0, 0, 0, 0, 12800, false, true, true, sourceName);
    }
    
    public bool IsValid()
    {
        return UEParse.Provider.Files.ContainsKey(UEParse.Provider.FixPath(MapPath + ".umap"));
    }
}