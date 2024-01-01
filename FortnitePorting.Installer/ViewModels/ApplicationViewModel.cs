using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Services;
using FortnitePorting.Framework.ViewModels;
using FortnitePorting.Installer.Views;

namespace FortnitePorting.Installer.ViewModels;

public partial class ApplicationViewModel : ThemedViewModelBase
{
    [ObservableProperty] private string versionString = $"v{Globals.VERSION}";

    [ObservableProperty] private UserControl? currentView;

    public ApplicationViewModel()
    {
        ThemeVM = this;
        SetView<MainView>();
    }
    
    public void SetView<T>() where T : UserControl, new()
    {
        TaskService.RunDispatcher(() => CurrentView = new T());
    }
}