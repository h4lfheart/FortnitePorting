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
using FortnitePorting.Multiplayer.Extensions;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;
using RestSharp;
using MiscExtensions = FortnitePorting.Shared.Extensions.MiscExtensions;

namespace FortnitePorting.ViewModels.Settings;

public partial class DiscordSettingsViewModel : ViewModelBase
{
   [ObservableProperty] private Guid _id = Guid.NewGuid();
   [ObservableProperty] private OAuthResponse? _auth;
   [ObservableProperty] private bool _useIntegration = false;
   [ObservableProperty] private bool _useRichPresence = true;
   [ObservableProperty] private bool _hasReceivedFirstPrompt = false;
   
   [field: JsonIgnore]
   [ObservableProperty]
   [NotifyPropertyChangedFor(nameof(UserName))]
   [NotifyPropertyChangedFor(nameof(GlobalName))]
   [NotifyPropertyChangedFor(nameof(ProfilePictureURL))]
   private IdentificationResponse? _identification;
   
   [JsonIgnore] public string? UserName => Identification?.Username;
   [JsonIgnore] public string? GlobalName => Identification?.GlobalName;
   [JsonIgnore] public string? ProfilePictureURL => Identification is not null 
      ? $"https://cdn.discordapp.com/avatars/{Identification.Id}/{Identification.AvatarId}.png?size=128" 
      : null;

   public async Task LoadIdentification()
   {
      if (UseIntegration && Auth is not null)
      {
         await ApiVM.Discord.CheckAuthRefresh();
         Identification = await ApiVM.Discord.GetIdentificationAsync();
      }
   }

   public async Task Authenticate()
   {
      // actually authorize
      var request = new RestRequest("https://discord.com/oauth2/authorize");
      request.AddQueryParameter("client_id", "1233219769478680586");
      request.AddQueryParameter("response_type", "code");
      request.AddQueryParameter("redirect_uri", FortnitePortingAPI.DISCORD_POST_URL);
      request.AddQueryParameter("scope", "identify");
      request.AddQueryParameter("state", Id.ToString());

      Launch(ApiVM.GetUrl(request));

      // wait for authorization to retrieve token
      for (var i = 0; i < 30; i++)
      {
         var auth = await ApiVM.FortnitePorting.GetDiscordAuthAsync();
         if (auth is not null)
         {
            Auth = auth;
            UseIntegration = true;
            await LoadIdentification();
            Id = Identification.Id.ToFpGuid();
            if (GlobalChatService.WasStarted) GlobalChatService.Init();
            AppVM.Message("Discord Integration", $"Successfully authenticated user \"{UserName}\" via Discord.", severity: InfoBarSeverity.Success, closeTime: 2.5f);
            return;
         }

         await Task.Delay(1000);
      }
      
      
      AppVM.Message("Discord Integration", "Timed out while trying to authenticate. Please try again.", InfoBarSeverity.Error, autoClose: false);
   }
   
   public async Task Deauthenticate()
   {
      var removedUsername = UserName;
      UseIntegration = false;
      Auth = null;
      Identification = null;
      GlobalChatService.DeInit();
      AppVM.Message("Discord Integration", $"Successfully de-authenticated user \"{removedUsername}\" via Discord.", closeTime: 2.5f);
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
            PrimaryButtonCommand = new RelayCommand(async () => await AppSettings.Current.Discord.Authenticate())
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