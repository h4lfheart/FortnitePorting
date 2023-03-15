using System;
using DiscordRPC;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Services;

public static class DiscordService
{
    private const string ID = "1023964585482518621";

    private static DiscordRpcClient? Client;

    private static readonly Assets Assets = new() { LargeImageKey = "icon", LargeImageText = "Fortnite Porting" };

    private static readonly Timestamps Timestamp = new() { Start = DateTime.UtcNow };

    private static readonly RichPresence DefaultPresence = new()
    {
        Timestamps = Timestamp,
        Assets = Assets
    };

    private static bool IsInitialized;

    public static void Initialize()
    {
        if (IsInitialized) return;
        Client = new DiscordRpcClient(ID);
        Client.OnReady += (_, args) => Log.Information("Discord Rich Presence Started for {Username}#{Discriminator}", args.User.Username, args.User.Discriminator.ToString("D4"));
        Client.OnError += (_, args) => Log.Information("Discord Rich Presence Error {Type}: {Message}", args.Type.ToString(), args.Message);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
        IsInitialized = true;
    }

    public static void DeInitialize()
    {
        if (!IsInitialized) return;
        var user = Client?.CurrentUser;
        Log.Information("Discord Rich Presence Stopped for {Username}#{Discriminator}", user?.Username, user?.Discriminator.ToString("D4"));
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
    
    public static void UpdateMusicState(string name)
    {
        if (!IsInitialized) return;
        Client?.UpdateState($"Vibin' out to \"{name}\"");
    }
    
    public static void ClearMusicState()
    {
        if (!IsInitialized) return;
        Client?.UpdateState(string.Empty);
    }
}