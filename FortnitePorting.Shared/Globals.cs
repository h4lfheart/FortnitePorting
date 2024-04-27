using Avalonia.Platform.Storage;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Shared.Models;

namespace FortnitePorting.Shared;

public static class Globals
{
    public static string VersionString => Version.GetDisplayString(EVersionStringType.IdentifierPrefix);
    public static readonly FPVersion Version = new(3, 0, 0, 0, "alpha");
    
    public static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings") { Patterns = [ "*.usmap" ] };
    
    // todo use api?
    public const EGame LatestGameVersion = EGame.GAME_UE5_4;
    
    public static readonly FGuid ZERO_GUID = new();
    public const string ZERO_CHAR = "0x0000000000000000000000000000000000000000000000000000000000000000";
    
    public const string DISCORD_URL = "https://discord.gg/FortnitePorting";
    public const string GITHUB_URL = "https://github.com/halfuwu/FortnitePorting/tree/v3";
    public const string KOFI_URL = "https://ko-fi.com/halfuwu";
    public const string WIKI_URL = "https://github.com/halfuwu/FortnitePorting/wiki";
}