using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ApplicationSettingsViewModel : ViewModelBase
{
   public string AssetPath => UseAssetsPath && Directory.Exists(AssetsPath) ? AssetsPath : AssetsFolder.FullName;
   
   [ObservableProperty] private bool _useAssetsPath;
   
   [NotifyDataErrorInfo]
   [DirectoryExists("Assets Path")]
   [ObservableProperty] private string _assetsPath;
   
   [ObservableProperty] private bool _useTabTransitions = true;

   [ObservableProperty] private int _chunkCacheLifetime = 1;

   [ObservableProperty] private FPVersion _lastOnlineVersion = Globals.Version;
    

   [JsonIgnore] public NavigationTransitionInfo Transition => UseTabTransitions ? new SlideNavigationTransitionInfo() : new SuppressNavigationTransitionInfo();
   
   
   [RelayCommand]
   public async Task BrowseAssetsPath()
   {
      if (await BrowseFolderDialog() is { } path)
      {
         AssetsPath = path;
      }
   }
}