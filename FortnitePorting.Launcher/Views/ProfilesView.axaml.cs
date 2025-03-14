using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Launcher.ViewModels;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Launcher.Views;

public partial class ProfilesView : ViewBase<ProfilesViewModel>
{
    public ProfilesView() : base(AppSettings.Current.Profiles, initializeViewModel: false)
    {
        InitializeComponent();
    }
}