using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Launcher.ViewModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.Views;

public partial class ProfilesView : ViewBase<ProfilesViewModel>
{
    public ProfilesView() : base(AppSettings.Current.Profiles)
    {
        InitializeComponent();
        
        ViewModelRegistry.NewOrExisting<RepositoriesViewModel>(initialize: true);
        ViewModelRegistry.NewOrExisting<DownloadsViewModel>(initialize: true);
    }
}