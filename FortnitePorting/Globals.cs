global using static FortnitePorting.Services.ApplicationService;
global using Serilog;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace FortnitePorting;

public static class Globals
{
    public const string VERSION = "1.1.2.2";

    public const string DISCORD_URL = "https://discord.gg/DZ5YFXdBA6";
    public const string GITHUB_URL = "https://github.com/halfuwu/FortnitePorting";
    public const string KOFI_URL = "https://ko-fi.com/halfuwu";

    public const string LOCALHOST = "127.0.0.1";
    public const int BLENDER_PORT = 24280;
    public const int UNREAL_PORT = 24281;
    public const int BUFFER_SIZE = 1024;

    public const string UDPClient_MessageTerminator = "MessageFinished";
    public const string UDPClient_Ping = "Ping";

    public const string WHITE = "#e1e9f2";
    public const string BLACK = "#000000";
    public const string BLUE = "#4b8ad1";
    public const string RED = "#d14b68";
    public const string YELLOW = "#d1c84b";
    public const string GREEN = "#03fc5e";

    public static readonly FGuid ZERO_GUID = new();
    public static readonly string ZERO_CHAR = "0x0000000000000000000000000000000000000000000000000000000000000000";
}