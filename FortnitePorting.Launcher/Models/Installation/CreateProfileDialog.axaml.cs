using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.Downloads;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Launcher.Models.Installation;

public partial class CreateProfileDialog : UserControl
{
    public CreateProfileDialog()
    {
        InitializeComponent();
        DataContext = new CreateProfileDialogContext();
    }
}

public partial class CreateProfileDialogContext : ObservableObject
{
    [ObservableProperty] private EProfileType _profileType;

    // repository
    [ObservableProperty] private DownloadRepository _selectedRepository = RepositoriesVM.Repositories.FirstOrDefault()!;
    [ObservableProperty] private ObservableCollection<DownloadRepository> _repositories = RepositoriesVM.Repositories;
    
    // custom
    [ObservableProperty] private InstallationVersion _selectedVersion = AppSettings.Current.DownloadedVersions.FirstOrDefault()!;
    [ObservableProperty] private ObservableCollection<InstallationVersion> _versions = AppSettings.Current.DownloadedVersions;
    
}