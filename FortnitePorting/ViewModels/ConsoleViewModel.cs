using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Models.Serilog;
using FortnitePorting.Services;
using Serilog.Core;
using Serilog.Events;

namespace FortnitePorting.ViewModels;

public partial class ConsoleViewModel() : ViewModelBase
{
    [ObservableProperty] private InfoService _info;
    
    public ConsoleViewModel(InfoService info) : this()
    {
        Info = info;
    }
    
    [ObservableProperty] private ScrollViewer _scroll;
    
    public override async Task Initialize()
    {
        Info.Logs.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var isScrolledToEnd = Math.Abs(Scroll.Offset.Y - Scroll.Extent.Height + Scroll.Viewport.Height) < 500;
                if (isScrolledToEnd)
                    Scroll.ScrollToEnd();
            });
        };
    }

    public override async Task OnViewOpened()
    {
        Scroll.ScrollToEnd();
    }

    [RelayCommand]
    private async Task OpenLog()
    {
        App.Launch(Info.LogFilePath);
    }
    
    [RelayCommand]
    private async Task OpenLogsFolder()
    {
        App.LaunchSelected(Info.LogFilePath);
    }
}
