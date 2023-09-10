using System;
using DiscordRPC;
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
            LargeImageText = $"Fortnite Porting v{Globals.VERSION}", 
            LargeImageKey = "large-icon"
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
}