using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Microsoft.VisualBasic.Logging;
using OpenTK.Graphics.OpenGL;
using Supabase;
using Supabase.Gotrue;
using Tomlyn;
using Client = Supabase.Client;
using Log = Serilog.Log;

namespace FortnitePorting.Services;

public partial class SupabaseService : ObservableObject, IService
{
    [ObservableProperty] private Client _client;
    [ObservableProperty] private bool _isActive;
    
    [ObservableProperty] private UserInfoResponse? _userInfo;

    private bool PostedLogin;
    
    private static readonly SupabaseOptions DefaultOptions = new()
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true
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
                var session = await Client.Auth.SetSession(sessionInfo.AccessToken, sessionInfo.RefreshToken);
                AppSettings.Online.SessionInfo = new UserSessionInfo(session.AccessToken, session.RefreshToken);
                await LoadUserInfo();
                
                IsActive = true;
                if (!PostedLogin)
                    await PostLogin();
            }

        });
    }

    public async Task SignIn()
    {
        var authState = await Client.Auth.SignIn(Constants.Provider.Discord, new SignInOptions
        {
            FlowType = Constants.OAuthFlowType.PKCE,
            RedirectTo = "http://localhost:19999"
        });
        
        App.Launch(authState.Uri.AbsoluteUri);
        
        using var authListener = new HttpListener();
        authListener.Prefixes.Add("http://localhost:19999/");
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
        AppSettings.Online.SessionInfo = new UserSessionInfo(session.AccessToken, session.RefreshToken);
        
        await LoadUserInfo();

        Info.Message("Discord Integration", $"Successfully signed in with discord user {UserInfo.UserName}");
        
        if (!PostedLogin)
            await PostLogin();
        
        IsActive = true;

    }
    
    public async Task SignOut()
    {
        Info.Message("Discord Integration", $"Successfully signed out with discord user {UserInfo.UserName}");
        
        AppSettings.Online.SessionInfo = null;
        UserInfo = null;
        IsActive = false;
    }

    public async Task PostExports(IEnumerable<string> objectPaths)
    {
        await Client.From<Export>().Insert(new Export
        {
            ExportPaths = objectPaths
        });
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
        UserInfo = await AppServices.Api.FortnitePortingV2.UserInfo(Client.Auth.CurrentUser!.Id!);
    }
}