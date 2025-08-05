using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase;
using FortnitePorting.Models.Supabase.User;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using MiscExtensions = FortnitePorting.Shared.Extensions.MiscExtensions;

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