using System;
using DiscordRPC;
using FortnitePorting.Framework.Extensions;
using Serilog;

namespace FortnitePorting.Services;

public static class DiscordService
{
    private const string ID = "1150461433449042053";

    private static bool IsInitialized;
    private static DiscordRpcClient Client = null!;

    private static readonly RichPresence DefaultPresence = new()
    {
        Timestamps = new Timestamps
        {
            Start = DateTime.UtcNow
        },
        Assets = new Assets
        {
            LargeImageText = $"Fortnite Porting v{Globals.VersionString}",
            LargeImageKey = "logo"
        },
        Buttons = new[]
        {
            new Button
            {
                Label = "Join FortnitePorting",
                Url = Globals.DISCORD_URL
            }
        }
    };

    public static void Initialize()
    {
        if (IsInitialized) return;

        Client = new DiscordRpcClient(ID);
        Client.OnReady += (_, args) => Log.Information("Discord Rich Presence Started for {Username} ({ID})", args.User.Username, args.User.ID);
        Client.OnError += (_, args) => Log.Information("Discord Rich Presence Error {Type}: {Message}", args.Type.ToString(), args.Message);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
        IsInitialized = true;
    }

    public static void Deinitialize()
    {
        if (!IsInitialized) return;

        var user = Client.CurrentUser;
        Log.Information("Discord Rich Presence Stopped for {Username} ({ID})", user.Username, user.ID);

        Client.Deinitialize();
        Client.Dispose();
        IsInitialized = false;
    }

    public static void Update(EAssetType assetType)
    {
        if (!IsInitialized) return;

        Client.UpdateState($"Browsing {assetType.GetDescription()}");
        Client.UpdateSmallAsset(assetType.ToString().ToLower(), assetType.GetDescription());
    }

    public static void Update(string name, string iconKey)
    {
        if (!IsInitialized) return;

        Client.UpdateState($"Browsing {name}");
        Client.UpdateSmallAsset(iconKey, name);
    }
    
    public static void UpdateMusic(string name)
    {
        if (!IsInitialized) return;

        Client.UpdateState($"Listening to \"{name}\"");
        Client.UpdateSmallAsset("music", "Music");
    }
}