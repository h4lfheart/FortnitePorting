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
        State = "Idle",
        Timestamps = Timestamp,
        Assets = Assets,
        Buttons = new []
        {
            new Button { Label = "Github Repository", Url = Globals.GITHUB_URL},
            new Button { Label = "Discord Server", Url = Globals.DISCORD_URL}
        }

    };
    
    public static void Initialize()
    {
        if (Client is not null && !Client.IsDisposed) return;
        
        Client = new DiscordRpcClient(ID);
        Client.Logger = new ConsoleLogger { Level = LogLevel.Warning };
        Client.OnReady += (_, args) => Log.Information("Discord Service Started for {0}#{1}", args.User.Username, args.User.Discriminator);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
    }
    
    public static void DeInitialize()
    {
        var user = Client?.CurrentUser;
        Log.Information("Discord Service Stopped for {0}#{1}", user?.Username, user?.Discriminator);
        Client?.Deinitialize();
        Client?.Dispose();
    }

    public static void Update(EAssetType assetType)
    {
        Client?.UpdateState($"Browsing {assetType.GetDescription()}");
        Client?.UpdateSmallAsset(assetType.ToString().ToLower(), assetType.GetDescription());
    }

}