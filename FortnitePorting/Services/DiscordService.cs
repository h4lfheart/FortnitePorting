using System;
using System.Data;
using DiscordRPC;
using DiscordRPC.Logging;
using FortnitePorting.Views.Extensions;
using Serilog;

namespace FortnitePorting.Services;

public static class DiscordService
{
    private const string ID = "1023964585482518621";
    
    private static DiscordRpcClient? Client;
    
    private static readonly Assets Assets = new() { LargeImageKey = "icon", LargeImageText = "Fortnite Porting"};
    
    private static readonly Timestamps Timestamp = new() { Start = DateTime.UtcNow };

    private static readonly RichPresence DefaultPresence = new()
    {
        Timestamps = Timestamp,
        Assets = Assets,
        Buttons = new []
        {
            new Button { Label = "GitHub Repository", Url = Globals.GITHUB_URL},
            new Button { Label = "Discord Server", Url = Globals.DISCORD_URL}
        }

    };

    private static bool IsInitialized;
    
    public static void Initialize()
    {
        if (IsInitialized) return;
        Client = new DiscordRpcClient(ID);
        Client.OnReady += (_, args) => Log.Information("Discord Rich Presence Started for {0}#{1}", args.User.Username, args.User.Discriminator.ToString("D4"));
        Client.OnError += (_, args) => Log.Information("Discord Rich Presence Error {0}: {1}", args.Type.ToString(), args.Message);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
        IsInitialized = true;
    }
    
    public static void DeInitialize()
    {
        var user = Client?.CurrentUser;
        Log.Information("Discord Rich Presence Stopped for {0}#{1}", user?.Username, user?.Discriminator.ToString("D4"));
        Client?.Deinitialize();
        Client?.Dispose();
        IsInitialized = false;
    }

    public static void Update(EAssetType assetType)
    {
        if (!IsInitialized) return;
        Client?.UpdateDetails($"Browsing {assetType.GetDescription()}");
        Client?.UpdateSmallAsset(assetType.ToString().ToLower(), assetType.GetDescription());
    }

}