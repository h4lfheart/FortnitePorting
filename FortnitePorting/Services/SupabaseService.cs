using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Levels;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Models.Supabase.User;
using FortnitePorting.Views.Settings;
using Mapster;
using Serilog;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Realtime.PostgresChanges;
using Client = Supabase.Client;

namespace FortnitePorting.Services;

public partial class SupabaseService : ObservableObject, IService
{
    [ObservableProperty] private APIService _api;

    public SupabaseService(APIService api)
    {
        Api = api;
        
        TaskService.Run(async () =>
        {
            var auth = await Api.FortnitePorting.Auth();
            if (auth is null)
            {
                Info.Message("Online Services", "Failed to retrieve authentication information.", InfoBarSeverity.Error);
                return;
            }

            Client = new Client(auth.SupabaseURL, auth.SupabaseAnonKey, DefaultOptions);
            
            Client.Auth.AddStateChangedListener(async (client, state) =>
            {
                if (client.CurrentSession is { } session)
                {
                    AppSettings.Account.SessionInfo = new UserSessionInfo(
                        session.AccessToken!, 
                        session.RefreshToken!
                    );
                }

                if (state == Constants.AuthState.SignedIn && !IsLoggedIn)
                {
                    await OnLoggedIn();
                }
            });
            
            await Client.InitializeAsync();

            if (AppSettings.Account.SessionInfo is { } sessionInfo)
            {
                await SetSession(sessionInfo);
            }
        });
    }
    
    [ObservableProperty] private Client _client;
    [ObservableProperty] private bool _isLoggedIn;
    
    [ObservableProperty] private UserInfoResponse? _userInfo;
    [ObservableProperty] private LevelStats? _levelStats;
    [ObservableProperty] private LevelStats? _prevLevelStats;
    [ObservableProperty] private UserPermissions _permissions = new();

    public event EventHandler<int> LevelUp; 
    
    private ProviderAuthState? _currentAuthState;
    private bool _postedLogin;
    
    private static readonly SupabaseOptions DefaultOptions = new()
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true,
    };

    public async Task SetSession(UserSessionInfo sessionInfo)
    {
        try
        {
            if (await Client.Auth.SetSession(sessionInfo.AccessToken, sessionInfo.RefreshToken) is { } session)
            {
                AppSettings.Account.SessionInfo = new UserSessionInfo(
                    session.AccessToken!, 
                    session.RefreshToken!
                );
            }
        }
        catch (GotrueException e) when (e.Message.Contains("refresh_token_already_used"))
        {
            AppSettings.Account.SessionInfo = null;
            IsLoggedIn = false;
            Info.Dialog("Session Expired", "Your session has expired. Please log in again.");
            Log.Warning("Refresh token already used, session cleared");
        }
        catch (Exception e)
        {
            AppSettings.Account.SessionInfo = null;
            IsLoggedIn = false;
            Info.Dialog("Online Services", "The online session is invalid, please log in again.");
            Log.Error(e.ToString());
        }
    }

    public async Task SignIn()
    {
        _currentAuthState = await Client.Auth.SignIn(Constants.Provider.Discord, new SignInOptions
        {
            FlowType = Constants.OAuthFlowType.PKCE,
            RedirectTo = "https://api.fortniteporting.app/v1/auth/redirect",
        });
        
        App.Launch(_currentAuthState.Uri.AbsoluteUri);

        while (!IsLoggedIn)
        {
            await Task.Delay(1);
        }
    }
    
    public async Task SignOut()
    {
        // TODO on sign out event to decouple ui responsiblities
        if (Navigation.Settings.IsTabOpen<AccountSettingsView>())
            Navigation.Settings.Open<ApplicationSettingsView>();

        await Chat.Uninitialize();

        AppSettings.Account.SessionInfo = null;
        UserInfo = null;
        IsLoggedIn = false;
        
        await Client.Auth.SignOut();
    }

    public async Task ExchangeCode(string code)
    {
        var session = await Client.Auth.ExchangeCodeForSession(_currentAuthState!.PKCEVerifier!, code);
        if (session is null)
        {
            Info.Message("Discord Integration", "Failed to sign in with discord.", severity: InfoBarSeverity.Error);
        }
    }

    public async Task PostExports(IEnumerable<string> objectPaths)
    {
        await Api.FortnitePorting.PostExports(objectPaths);
    }

    private async Task OnLoggedIn()
    {
        IsLoggedIn = true;
        
        Permissions = (await Client.Rpc<Permissions>("permissions", new { })).Adapt<UserPermissions>();
        
        await Client.From<Permissions>().On(PostgresChangesOptions.ListenType.All, (channel, response) =>
        {
            Permissions = response.Model<Permissions>().Adapt<UserPermissions>();
        });
        
        LevelStats = await Client.CallObjectFunction<LevelStats>("get_level_stats");
        
        await Client.From<Levels>().On(PostgresChangesOptions.ListenType.All, async (channel, response) =>
        {
            PrevLevelStats = LevelStats;
            LevelStats = await Client.CallObjectFunction<LevelStats>("get_level_stats");

            if (PrevLevelStats?.Level != LevelStats?.Level)
            {
                LevelUp?.Invoke(this, LevelStats?.Level ?? 0);
            }
        });
            
        await LoadUserInfo();

        await PostLogin();

        await Chat.Initialize();
    }
    
    private async Task PostLogin()
    {
        if (_postedLogin) return;

        await Api.FortnitePorting.PostLogin();
        _postedLogin = true;
    }

    private async Task LoadUserInfo()
    {
        UserInfo = await AppServices.Api.FortnitePorting.UserInfo(Client.Auth.CurrentUser!.Id!);
    }
}