using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Application;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models.API;
using FortnitePorting.Shared.Services;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using MiscExtensions = FortnitePorting.Shared.Extensions.MiscExtensions;

namespace FortnitePorting.ViewModels.Settings;

public partial class OnlineSettingsViewModel : ViewModelBase
{
   [ObservableProperty] private AuthResponse? _auth;
   [ObservableProperty] private bool _useIntegration = false;
   [ObservableProperty] private bool _useRichPresence = true;
   [ObservableProperty] private bool _hasReceivedFirstPrompt = false;
   [ObservableProperty] private int _messageFetchCount = 50;
   [ObservableProperty] private EOnlineStatus _onlineStatus = EOnlineStatus.Online;

   [ObservableProperty] private EpicAuthResponse? _epicAuth;
   
   [field: JsonIgnore]
   [ObservableProperty]
   [NotifyPropertyChangedFor(nameof(UserName))]
   [NotifyPropertyChangedFor(nameof(GlobalName))]
   [NotifyPropertyChangedFor(nameof(ProfilePictureURL))]
   private UserResponse? _identification;
   
   [JsonIgnore] public string? UserName => Identification?.Username;
   [JsonIgnore] public string? GlobalName => !string.IsNullOrWhiteSpace(Identification?.GlobalName) ? Identification.GlobalName : Identification?.Username;

   [JsonIgnore]
   public string? ProfilePictureURL => Globals.GetSeededOGProfileURL(!string.IsNullOrWhiteSpace(Identification?.AvatarId)
      ? $"https://cdn.discordapp.com/avatars/{Identification.DiscordId}/{Identification.AvatarId}.png?size=128"
      : "https://fortniteporting.halfheart.dev/logo/default.png");
   

   public async Task LoadIdentification()
   {
      if (UseIntegration && Auth is not null)
      {
         await ApiVM.FortnitePorting.RefreshAuthAsync();
         Identification = await ApiVM.FortnitePorting.GetUserAsync(Auth.Token);
      }
   }

   public async Task Authenticate()
   {
      var state = Guid.NewGuid();
      
      // actually authorize
      var request = new RestRequest("https://discord.com/oauth2/authorize");
      request.AddQueryParameter("client_id", "1233219769478680586");
      request.AddQueryParameter("response_type", "code");
      request.AddQueryParameter("redirect_uri", FortnitePortingAPI.AUTH_REDIRECT_URL);
      request.AddQueryParameter("scope", "identify");
      request.AddQueryParameter("state", state.ToString());

      Launch(ApiVM.GetUrl(request));

      // wait for authorization to retrieve token
      for (var i = 0; i < 30; i++)
      {
         var auth = await ApiVM.FortnitePorting.GetAuthAsync(state);
         
         if (auth is not null)
         {
            OnlineService.DeInit();
            Auth = auth;
            UseIntegration = true;
            await LoadIdentification();
            OnlineService.Init();
            AppWM.OnlineAndGameTabsAreVisible = true;
            AppWM.Message("Discord Integration", $"Successfully authenticated user \"{UserName}\" via Discord.", severity: InfoBarSeverity.Success, closeTime: 2.5f);
            
            AppSettings.Save();
            return;
         }

         await Task.Delay(1000);
      }
      
      AppWM.Message("Discord Integration", "Timed out while trying to authenticate. Please try again.", InfoBarSeverity.Error, autoClose: false);
   }
   
   public async Task Deauthenticate()
   {
      var removedUsername = UserName;
      UseIntegration = false;
      Auth = null;
      Identification = null;
      OnlineService.DeInit();
      AppWM.OnlineAndGameTabsAreVisible = false;
      AppWM.Message("Discord Integration", $"Successfully de-authenticated user \"{removedUsername}\" via Discord.", closeTime: 2.5f);
      AppSettings.Save();
   }

   public async Task PromptForAuthentication()
   {
      await TaskService.RunDispatcherAsync(async () =>
      {
         HasReceivedFirstPrompt = true;
         var discordIntegrationDialog = new ContentDialog
         {
            Title = "Discord Integration",
            Content = "To use any of FortnitePorting's online features, you must authenticate yourself via Discord.\n\n" +
                      "Would you like to authenticate now?\n\n" +
                      "This choice can be changed any time in the application settings.",
            CloseButtonText = "Ignore",
            PrimaryButtonText = "Authenticate",
            PrimaryButtonCommand = new RelayCommand(async () => await AppSettings.Current.Online.Authenticate())
         };

         await discordIntegrationDialog.ShowAsync();
      });
   }

   protected override void OnPropertyChanged(PropertyChangedEventArgs e)
   {
      base.OnPropertyChanged(e);

      switch (e.PropertyName)
      {
         case nameof(UseRichPresence):
         {
            if (UseRichPresence)
            {
               DiscordService.Initialize();
            }
            else
            {
               DiscordService.Deinitialize();
            }
            
            break;
         }
      }
   }
}

public enum EOnlineStatus
{
   [Description("Online")]
   Online,
   
   [Description("Do Not Disturb")]
   DoNotDisturb
}