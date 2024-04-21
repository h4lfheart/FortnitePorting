using Avalonia.Platform.Storage;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Shared.Models;

namespace FortnitePorting.Shared;

public static class Globals
{
    public static string VersionString => $"v{Version}";
    public static readonly FPVersion Version = new(3, 0, 0, 1, "alpha");
    
    public static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings") { Patterns = [ "*.usmap" ] };
    
    // todo use api?
    public const EGame LatestGameVersion = EGame.GAME_UE5_4;
    
    public static readonly FGuid ZERO_GUID = new();
    public const string ZERO_CHAR = "0x0000000000000000000000000000000000000000000000000000000000000000";
}