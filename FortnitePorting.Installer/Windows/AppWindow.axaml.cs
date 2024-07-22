using System;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Installer.WindowModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Installer.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
    }
}