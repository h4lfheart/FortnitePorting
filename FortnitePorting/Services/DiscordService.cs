using System;
using DiscordRPC;
using FortnitePorting.Shared;
using Serilog;

namespace FortnitePorting.Services;

public static class DiscordService
{
    private static bool IsInitialized;
    public static DiscordRpcClient? Client;

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
        Buttons = 
        [
            new Button
            {
                Label = "Join FortnitePorting",
                Url = Globals.DISCORD_URL
            }
        ]
    };
    
    private const string ID = "1233219769478680586";
    
    public static void Initialize()
    {
        if (IsInitialized) return;

        Client = new DiscordRpcClient(ID);
        Client.OnReady += (_, args) =>
        {
            Log.Information("Discord Rich Presence Started for {Username} ({ID})", args.User.Username, args.User.ID);
            IsInitialized = true;
        };
        Client.OnError += (_, args) => Log.Information("Discord Rich Presence Error {Type}: {Message}", args.Type.ToString(), args.Message);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
    }

    public static void Deinitialize()
    {
        if (!IsInitialized) return;

        var user = Client!.CurrentUser;
        Log.Information("Discord Rich Presence Stopped for {Username} ({ID})", user.Username, user.ID);

        Client.Deinitialize();
        Client.Dispose();
        IsInitialized = false;
    }
    
    public static string? GetUserName()
    {
        return Client?.CurrentUser?.Username;
    }
    
    public static string? GetDisplayName()
    {
        return Client?.CurrentUser?.DisplayName;
    }
    
    public static string? GetAvatarURL()
    {
        return Client?.CurrentUser?.GetAvatarURL(User.AvatarFormat.PNG, User.AvatarSize.x128);
    }
}