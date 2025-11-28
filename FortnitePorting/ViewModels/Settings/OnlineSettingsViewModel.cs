using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Supabase.User;
using FortnitePorting.Services;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class OnlineSettingsViewModel : ViewModelBase
{
   [JsonIgnore] public SupabaseService SupaBase => AppServices.SupaBase;

   [ObservableProperty] private UserSessionInfo? _sessionInfo;

   [ObservableProperty] private bool _useDiscordRichPresence = true;

   partial void OnUseDiscordRichPresenceChanged(bool value)
   {
      if (value)
         Discord.Initialize();
      else
         Discord.Deinitialize();
   }
}