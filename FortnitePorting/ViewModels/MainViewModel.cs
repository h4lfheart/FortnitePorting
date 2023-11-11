using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private UserControl activeTab;
    [ObservableProperty] private bool assetTabReady;
    [ObservableProperty] private bool meshTabReady;
    [ObservableProperty] private bool animateTabChanges = AppSettings.Current.UseTabTransition;
}