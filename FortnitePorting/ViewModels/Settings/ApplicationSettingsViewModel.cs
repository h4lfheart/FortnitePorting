using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ApplicationSettingsViewModel : ViewModelBase
{
   [ObservableProperty] private bool _useAssetsPath;
   [ObservableProperty] private string _assetsPath;
   [ObservableProperty] private bool _useTabTransitions = true;
   [ObservableProperty] private bool _useDiscordRPC = true;

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