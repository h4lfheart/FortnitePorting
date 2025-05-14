using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Models.Supabase.User;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Mapster;
using Microsoft.VisualBasic.Logging;
using OpenTK.Graphics.OpenGL;
using Supabase;
using Supabase.Gotrue;
using Supabase.Realtime.PostgresChanges;
using Tomlyn;
using Client = Supabase.Client;
using Log = Serilog.Log;

namespace FortnitePorting.Services;

public partial class SupabaseService : ObservableObject, IService
{
    [ObservableProperty] private Client _client;
    [ObservableProperty] private bool _isLoggedIn;
    
    [ObservableProperty] private UserInfoResponse? _userInfo;
    [ObservableProperty] private UserPermissions _permissions;

    private bool PostedLogin;
    
    private static readonly SupabaseOptions DefaultOptions = new()
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true,
    };
    
    public SupabaseService()
    {
        var dataStream = AssetLoader.Open(new Uri("avares://FortnitePorting/supabase.toml"));
        var dataReader = new StreamReader(dataStream);
        var data = Toml.ToModel(dataReader.ReadToEnd());

        Client = new Client((string)data["SUPABASE_URL"], (string)data["SUPABASE_ANON_KEY"], DefaultOptions);
        
        TaskService.Run(async () =>
        {
            await Client.InitializeAsync();

            if (AppSettings.Online.SessionInfo is { } sessionInfo)
            {
                await SetSession(sessionInfo);
            }

        });
    }

    public async Task SetSession(UserSessionInfo sessionInfo)
    {
        var session = await Client.Auth.SetSession(sessionInfo.AccessToken, sessionInfo.RefreshToken);
                
        await OnLoggedIn();

        AppSettings.Online.SessionInfo = new UserSessionInfo(session.AccessToken!, session.RefreshToken!);
    }

    public async Task SignIn()
    {
        var authState = await Client.Auth.SignIn(Constants.Provider.Discord, new SignInOptions
        {
            FlowType = Constants.OAuthFlowType.PKCE,
            RedirectTo = "http://localhost:24000"
        });
        
        App.Launch(authState.Uri.AbsoluteUri);
        
        using var authListener = new HttpListener();
        authListener.Prefixes.Add("http://localhost:24000/");
        authListener.Start();

        string? code = null;
        while (code is null)
        {
            var context = await authListener.GetContextAsync();
                
            context.Response.OutputStream.Write("Successfully authenticated with discord."u8);
            context.Response.OutputStream.Close(); 
                
            code = context.Request.QueryString.Get("code");
        }
        
        authListener.Stop();
        
        var session = await Client.Auth.ExchangeCodeForSession(authState.PKCEVerifier!, code);
        if (session is null)
        {
            Info.Message("Discord Integration", "Failed to sign in with discord.", severity: InfoBarSeverity.Error);
            return;
        }
        
        AppSettings.Online.SessionInfo = new UserSessionInfo(session.AccessToken!, session.RefreshToken!);

        await OnLoggedIn();

        Info.Message("Discord Integration", $"Successfully signed in discord user {UserInfo!.UserName}");

    }
    
    public async Task SignOut()
    {
        Info.Message("Discord Integration", $"Successfully signed out discord user {UserInfo!.UserName}");

        await Chat.Uninitialize();

        AppSettings.Online.SessionInfo = null;
        UserInfo = null;
        IsLoggedIn = false;
        
        await Client.Auth.SignOut();
    }

    public async Task PostExports(IEnumerable<string> objectPaths)
    {
        await Client.From<Export>().Insert(new Export
        {
            ExportPaths = objectPaths
        });
    }

    private async Task OnLoggedIn()
    {
        IsLoggedIn = true;
                
        await LoadUserInfo();
                
        if (!PostedLogin)
            await PostLogin();

        await Client.From<Permissions>().On(PostgresChangesOptions.ListenType.All, (channel, response) =>
        {
            Permissions = response.Model<Permissions>().Adapt<UserPermissions>();
        });

        Permissions = (await Client.Rpc<Permissions>("permissions", new { })).Adapt<UserPermissions>();
        
        await VotingVM.Initialize();
        await Chat.Initialize();
    }
    
    private async Task PostLogin()
    {
        await Client.From<Login>().Insert(new Login
        {
            Version = Globals.Version.GetDisplayString()
        });
    }

    private async Task LoadUserInfo()
    {
        UserInfo = await AppServices.Api.FortnitePorting.UserInfo(Client.Auth.CurrentUser!.Id!);
    }
}