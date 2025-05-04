using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Supabase;
using Supabase.Gotrue;
using Tomlyn;
using Client = Supabase.Client;

namespace FortnitePorting.Services;

public partial class SupabaseService : ObservableObject, IService
{
    
    [ObservableProperty] private UserInfoResponse? _userInfo;
    
    public Client Client;

    public bool IsActive => Client.Auth.CurrentUser is not null;

    
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
    }
    
    public async Task SignOut()
    {
        AppSettings.Online.SessionInfo = null;
    }

    private async Task LoadUserInfo()
    {
        UserInfo = await AppServices.Api.FortnitePortingV2.UserInfo(Client.Auth.CurrentUser!.Id!);
    }
}