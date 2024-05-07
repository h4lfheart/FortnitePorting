global using static FortnitePorting.Application.App;
global using static FortnitePorting.Framework.Application.AppBase;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Framework.ViewModels.Endpoints.Models;

namespace FortnitePorting;

public static class Globals
{
    public static readonly FPVersion Version = new(2, 1, 3);
    public static readonly string VersionString = Version.ToString();

    public const string DISCORD_URL = "https://discord.gg/DZ5YFXdBA6";
    public const string GITHUB_URL = "https://github.com/halfuwu/FortnitePorting/tree/v2";
    public const string KOFI_URL = "https://ko-fi.com/halfuwu";
    public const string WIKI_URL = "https://github.com/halfuwu/FortnitePorting/wiki";

    public static readonly FGuid ZERO_GUID = new();
    public const string ZERO_CHAR = "0x0000000000000000000000000000000000000000000000000000000000000000";
    public const EGame LatestGameVersion = EGame.GAME_UE5_4;
}