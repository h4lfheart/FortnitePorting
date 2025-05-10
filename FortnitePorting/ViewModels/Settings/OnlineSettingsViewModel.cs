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
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
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
   [JsonIgnore] public SupabaseService SupaBase => AppServices.SupaBase;
   
   [ObservableProperty, NotifyPropertyChangedFor(nameof(CurrentSessionInfo))] private ObservableCollection<UserSessionInfo> _sessionInfos = [];
   [ObservableProperty, NotifyPropertyChangedFor(nameof(CurrentSessionInfo))] private int _selectedSessionIndex = 0;

   [JsonIgnore]
   public UserSessionInfo? CurrentSessionInfo => 
      SelectedSessionIndex >= 0 && SelectedSessionIndex < SessionInfos.Count ? SessionInfos[SelectedSessionIndex] : null;

   [ObservableProperty] private bool _askedFirstTimePopup;

   protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
   {
      base.OnPropertyChanged(e);

      switch (e.PropertyName)
      {
         case nameof(SelectedSessionIndex) when CurrentSessionInfo is not null:
         {
            await SupaBase.SetSession(CurrentSessionInfo);
            break;
         }
      }
   }
}