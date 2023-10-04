using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private UserControl activeTab;
}